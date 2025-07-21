import * as cdk from 'aws-cdk-lib';
import * as ssm from 'aws-cdk-lib/aws-ssm';
import * as dynamodb from 'aws-cdk-lib/aws-dynamodb';
import * as route53 from 'aws-cdk-lib/aws-route53';
import * as acm from 'aws-cdk-lib/aws-certificatemanager';
import * as s3 from 'aws-cdk-lib/aws-s3';
import * as cloudfront from 'aws-cdk-lib/aws-cloudfront';
import * as origins from 'aws-cdk-lib/aws-cloudfront-origins';
import * as targets from 'aws-cdk-lib/aws-route53-targets';
import * as s3deploy from 'aws-cdk-lib/aws-s3-deployment';
import * as iam from 'aws-cdk-lib/aws-iam';
import * as ecs from 'aws-cdk-lib/aws-ecs';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import * as ecr from 'aws-cdk-lib/aws-ecr';
import * as elbv2 from 'aws-cdk-lib/aws-elasticloadbalancingv2';
import * as logs from 'aws-cdk-lib/aws-logs';
import { RemovalPolicy } from 'aws-cdk-lib';
import {
  PawfectMatchBaseStackProps,
  StageType,
  DomainUtils,
  ClientHostingConfig,
} from './utils';
import { Construct } from 'constructs';
import { EnvironmentStack } from './environment-stack';

export interface DynamoDbTableConfig {
  tableName: string;
  partitionKey: {
    name: string;
    type: dynamodb.AttributeType;
  };
  sortKey?: {
    name: string;
    type: dynamodb.AttributeType;
  };
  globalSecondaryIndexes?: {
    indexName: string;
    partitionKey: {
      name: string;
      type: dynamodb.AttributeType;
    };
    sortKey?: {
      name: string;
      type: dynamodb.AttributeType;
    };
    nonKeyAttributes?: string[];
  }[];
  description?: string;
}

export class BaseStack extends cdk.Stack {
  protected readonly stage: StageType;
  protected readonly sharedStack: cdk.Stack;
  protected readonly environmentStack: EnvironmentStack;
  protected readonly serviceName: string;
  protected readonly hostedZone?: route53.IHostedZone;
  protected readonly certificate?: acm.ICertificate;
  protected readonly regionalCertificate?: acm.ICertificate;

  // Client hosting resources
  public clientBucket?: s3.Bucket;
  public clientDistribution?: cloudfront.Distribution;
  public clientDomainName?: string;

  // ECS Backend resources
  public ecsService?: ecs.FargateService;
  public applicationLoadBalancer?: elbv2.ApplicationLoadBalancer;
  public targetGroup?: elbv2.ApplicationTargetGroup;
  public apiDomainName?: string;

  constructor(scope: Construct, id: string, props: PawfectMatchBaseStackProps) {
    super(scope, id, props);

    this.stage = props.stage;
    this.sharedStack = props.sharedStack;
    this.environmentStack = props.environmentStack;
    this.serviceName = props.serviceName;

    // Import domain resources if they exist
    if (props.domainConfig && props.sharedStack) {
      try {
        // Get hosted zone from shared stack
        this.hostedZone = props.sharedStack.hostedZone;
        this.certificate = props.sharedStack.certificate;
        this.regionalCertificate = props.sharedStack.regionalCertificate;
      } catch (error) {
        console.warn(
          `Domain resources not available for ${this.serviceName}:`,
          error
        );
      }
    }

    // Setup client hosting if configured
    if (props.clientHosting) {
      this.createClientHosting(props.clientHosting);
    }
  }

  static getCapitalizedStage(stage: StageType): string {
    return stage.charAt(0).toUpperCase() + stage.slice(1);
  }

  /**
   * Creates an SSM parameter with a standardized naming convention
   * @param id The construct ID for the parameter
   * @param stage The deployment stage
   * @param service The service name (e.g., 'Identity', 'Matcher')
   * @param parameterPath The parameter name (e.g., 'UserPoolId', 'ApiKey')
   * @param value The parameter value
   * @param description Optional description for the parameter
   * @returns The created SSM StringParameter
   */
  protected createSsmParameter(
    parameterPath: string,
    value: string,
    description?: string
  ): ssm.StringParameter {
    const id = `${this.stackName}-${parameterPath.replace(/\//g, '-')}`;
    return new ssm.StringParameter(this, id, {
      parameterName: `/PawfectMatch/${BaseStack.getCapitalizedStage(
        this.stage
      )}/${this.serviceName}/${parameterPath}`,
      stringValue: value,
      description:
        description || `${parameterPath} for ${this.stage} environment`,
    });
  }

  /**
   * Creates a DynamoDB table with standardized configuration based on stage
   * @param id The construct ID for the table
   * @param stage The deployment stage
   * @param config The table configuration
   * @returns The created DynamoDB Table
   */
  protected createDynamoDbTable(config: DynamoDbTableConfig): dynamodb.Table {
    const tableName = `${this.stackName}-${config.tableName}`;

    const tableProps: dynamodb.TableProps = {
      tableName: tableName,
      partitionKey: config.partitionKey,
      ...(config.sortKey && { sortKey: config.sortKey }),
      billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
      pointInTimeRecoverySpecification: {
        pointInTimeRecoveryEnabled: this.stage === 'production',
      },
      deletionProtection: this.stage === 'production',
      removalPolicy:
        this.stage === 'production'
          ? RemovalPolicy.RETAIN
          : RemovalPolicy.DESTROY,
    };

    // Create the table
    const table = new dynamodb.Table(this, tableName, tableProps);

    // Add Global Secondary Indexes if provided
    if (config.globalSecondaryIndexes) {
      config.globalSecondaryIndexes.forEach((gsi) => {
        const gsiProps: dynamodb.GlobalSecondaryIndexProps = {
          indexName: gsi.indexName,
          partitionKey: gsi.partitionKey,
          ...(gsi.sortKey && { sortKey: gsi.sortKey }),
          projectionType: dynamodb.ProjectionType.INCLUDE,
          nonKeyAttributes: gsi.nonKeyAttributes,
        };

        table.addGlobalSecondaryIndex(gsiProps);
      });
    }

    return table;
  }

  /**
   * Creates client hosting resources (S3 + CloudFront + Route53) for Angular applications
   * @param config The client hosting configuration
   */
  protected createClientHosting(config: ClientHostingConfig): void {
    if (!config.enabled) {
      return;
    }

    // Determine the full domain name
    this.clientDomainName =
      this.stage === 'production'
        ? `${config.subdomain}.pawfectmatchnow.com`
        : `${config.subdomain}.${this.stage}.pawfectmatchnow.com`;

    // Create S3 bucket for hosting static website
    this.clientBucket = new s3.Bucket(this, `${this.serviceName}ClientBucket`, {
      bucketName: `${this.stackName}-${this.serviceName.toLowerCase()}-client`,
      websiteIndexDocument: 'index.csr.html',
      websiteErrorDocument: 'index.csr.html', // For SPA routing
      publicReadAccess: false, // We'll use CloudFront OAC instead
      blockPublicAccess: s3.BlockPublicAccess.BLOCK_ALL,
      removalPolicy:
        this.stage === 'production'
          ? RemovalPolicy.RETAIN
          : RemovalPolicy.DESTROY,
      autoDeleteObjects: this.stage !== 'production',
      versioned: this.stage === 'production',
      lifecycleRules:
        this.stage === 'production'
          ? [
              {
                id: 'DeleteIncompleteMultipartUploads',
                abortIncompleteMultipartUploadAfter: cdk.Duration.days(7),
              },
              {
                id: 'TransitionToIA',
                transitions: [
                  {
                    storageClass: s3.StorageClass.INFREQUENT_ACCESS,
                    transitionAfter: cdk.Duration.days(30),
                  },
                ],
              },
            ]
          : [],
    });

    // Create Origin Access Control for CloudFront
    const oac = new cloudfront.S3OriginAccessControl(
      this,
      `${this.serviceName}ClientOAC`,
      {
        description: `OAC for ${this.serviceName} client hosting`,
      }
    );

    // Create CloudFront distribution
    this.clientDistribution = new cloudfront.Distribution(
      this,
      `${this.serviceName}ClientDistribution`,
      {
        comment: `${this.serviceName} Client Distribution - ${this.stage}`,
        defaultRootObject: 'index.csr.html',
        priceClass: cloudfront.PriceClass.PRICE_CLASS_100, // Use only North America and Europe for cost optimization

        // Use certificate from shared stack if available
        ...(this.certificate && {
          certificate: this.certificate,
          domainNames: [this.clientDomainName],
        }),

        defaultBehavior: {
          origin: origins.S3BucketOrigin.withOriginAccessControl(
            this.clientBucket,
            {
              originAccessControl: oac,
            }
          ),
          viewerProtocolPolicy:
            cloudfront.ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
          allowedMethods: cloudfront.AllowedMethods.ALLOW_GET_HEAD,
          cachedMethods: cloudfront.CachedMethods.CACHE_GET_HEAD,
          compress: true,
          cachePolicy: cloudfront.CachePolicy.CACHING_OPTIMIZED,
        },

        // Handle SPA routing
        errorResponses: [
          {
            httpStatus: 403,
            responseHttpStatus: 200,
            responsePagePath: '/index.csr.html',
          },
          {
            httpStatus: 404,
            responseHttpStatus: 200,
            responsePagePath: '/index.csr.html',
          },
        ],

        // Enable additional configuration
        enableLogging: false,
      }
    );

    // Grant CloudFront access to the S3 bucket
    this.clientBucket.addToResourcePolicy(
      new iam.PolicyStatement({
        effect: iam.Effect.ALLOW,
        principals: [new iam.ServicePrincipal('cloudfront.amazonaws.com')],
        actions: ['s3:GetObject'],
        resources: [`${this.clientBucket.bucketArn}/*`],
        conditions: {
          StringEquals: {
            'AWS:SourceArn': `arn:aws:cloudfront::${this.account}:distribution/${this.clientDistribution.distributionId}`,
          },
        },
      })
    );

    // Create Route 53 record if we have a hosted zone and certificate
    if (this.hostedZone && this.certificate && this.clientDomainName) {
      new route53.ARecord(this, `${this.serviceName}ClientAliasRecord`, {
        zone: this.hostedZone,
        recordName: this.clientDomainName,
        target: route53.RecordTarget.fromAlias(
          new targets.CloudFrontTarget(this.clientDistribution)
        ),
      });
    }

    // Deploy the Angular app to S3
    new s3deploy.BucketDeployment(this, `${this.serviceName}ClientDeployment`, {
      sources: [s3deploy.Source.asset(config.distPath)],
      destinationBucket: this.clientBucket,
      distribution: this.clientDistribution,
      distributionPaths: ['/*'], // Invalidate all paths
    });

    // Create SSM parameters for client resources
    this.createSsmParameter(
      'ClientBucketName',
      this.clientBucket.bucketName,
      `S3 bucket name for ${this.serviceName} client`
    );

    this.createSsmParameter(
      'ClientDistributionId',
      this.clientDistribution.distributionId,
      `CloudFront distribution ID for ${this.serviceName} client`
    );

    this.createSsmParameter(
      'ClientDistributionDomainName',
      this.clientDistribution.distributionDomainName,
      `CloudFront distribution domain name for ${this.serviceName} client`
    );

    if (this.certificate && this.clientDomainName) {
      this.createSsmParameter(
        'ClientCustomDomainName',
        this.clientDomainName,
        `Custom domain name for ${this.serviceName} client`
      );
    }

    // Outputs
    new cdk.CfnOutput(this, `${this.serviceName}ClientBucketName`, {
      value: this.clientBucket.bucketName,
      description: `S3 bucket name for ${this.serviceName} client`,
    });

    new cdk.CfnOutput(this, `${this.serviceName}ClientDistributionId`, {
      value: this.clientDistribution.distributionId,
      description: `CloudFront distribution ID for ${this.serviceName} client`,
    });

    new cdk.CfnOutput(this, `${this.serviceName}ClientDistributionDomainName`, {
      value: this.clientDistribution.distributionDomainName,
      description: `CloudFront distribution domain name for ${this.serviceName} client`,
    });

    if (this.certificate && this.clientDomainName) {
      new cdk.CfnOutput(this, `${this.serviceName}ClientCustomDomainName`, {
        value: this.clientDomainName,
        description: `Custom domain name for ${this.serviceName} client`,
      });
    }

    // Add tags
    cdk.Tags.of(this.clientBucket).add('Service', this.serviceName);
    cdk.Tags.of(this.clientBucket).add('Component', 'ClientHosting');
    cdk.Tags.of(this.clientBucket).add('Environment', this.stage);

    cdk.Tags.of(this.clientDistribution).add('Service', this.serviceName);
    cdk.Tags.of(this.clientDistribution).add('Component', 'ClientHosting');
    cdk.Tags.of(this.clientDistribution).add('Environment', this.stage);
  }

  /**
   * Creates an ECS Fargate service for the backend API
   * @param config Configuration for the ECS service
   */
  protected createEcsService(config: {
    repository: ecr.IRepository;
    containerPort?: number;
    cpu?: number;
    memory?: number;
    healthCheckPath?: string;
    subdomain?: string;
    environment?: { [key: string]: string };
  }): void {
    const {
      repository,
      containerPort = 80,
      cpu = 256,
      memory = 512,
      healthCheckPath = '/health',
      subdomain = 'api',
      environment = {},
    } = config;

    // Get ECS resources from environment stack
    const cluster = this.environmentStack.ecsCluster;
    const vpc = this.environmentStack.vpc;
    const taskExecutionRole = this.environmentStack.taskExecutionRole;
    const taskRole = this.environmentStack.taskRole;
    const logGroup = this.environmentStack.logGroup;

    // Create security group for the service
    const serviceSecurityGroup = new ec2.SecurityGroup(
      this,
      'ServiceSecurityGroup',
      {
        vpc,
        description: `Security group for ${this.serviceName} ECS service`,
        allowAllOutbound: true,
      }
    );

    // Create ALB security group
    const albSecurityGroup = new ec2.SecurityGroup(this, 'AlbSecurityGroup', {
      vpc,
      description: `Security group for ${this.serviceName} ALB`,
      allowAllOutbound: true,
    });

    // Allow HTTP and HTTPS traffic to ALB
    albSecurityGroup.addIngressRule(
      ec2.Peer.anyIpv4(),
      ec2.Port.tcp(80),
      'Allow HTTP'
    );
    albSecurityGroup.addIngressRule(
      ec2.Peer.anyIpv4(),
      ec2.Port.tcp(443),
      'Allow HTTPS'
    );

    // Allow ALB to reach the service
    serviceSecurityGroup.addIngressRule(
      albSecurityGroup,
      ec2.Port.tcp(containerPort),
      'Allow ALB access to container'
    );

    // Create Application Load Balancer
    this.applicationLoadBalancer = new elbv2.ApplicationLoadBalancer(
      this,
      'ApplicationLoadBalancer',
      {
        vpc,
        internetFacing: true,
        securityGroup: albSecurityGroup,
      }
    );

    // Create target group
    this.targetGroup = new elbv2.ApplicationTargetGroup(this, 'TargetGroup', {
      vpc,
      port: containerPort,
      protocol: elbv2.ApplicationProtocol.HTTP,
      targetType: elbv2.TargetType.IP,
      healthCheck: {
        enabled: true,
        path: healthCheckPath,
        healthyHttpCodes: '200',
        interval: cdk.Duration.seconds(30),
        timeout: cdk.Duration.seconds(5),
        healthyThresholdCount: 2,
        unhealthyThresholdCount: 3,
      },
    });

    // Create task definition
    const taskDefinition = new ecs.FargateTaskDefinition(
      this,
      'TaskDefinition',
      {
        family: `${this.stackName}-task`,
        cpu,
        memoryLimitMiB: memory,
        executionRole: taskExecutionRole,
        taskRole,
        runtimePlatform: {
          cpuArchitecture: ecs.CpuArchitecture.ARM64,
          operatingSystemFamily: ecs.OperatingSystemFamily.LINUX,
        },
      }
    );

    // Add container to task definition
    const container = taskDefinition.addContainer('Container', {
      image: ecs.ContainerImage.fromEcrRepository(repository, 'latest'),
      logging: ecs.LogDrivers.awsLogs({
        streamPrefix: this.serviceName,
        logGroup,
      }),
      environment: {
        ASPNETCORE_ENVIRONMENT:
          this.stage === 'production' ? 'Production' : 'Development',
        ASPNETCORE_URLS: `http://+:${containerPort}`,
        ...environment,
      },
      essential: true,
    });

    // Add port mapping
    container.addPortMappings({
      containerPort,
      protocol: ecs.Protocol.TCP,
    });

    // Create ECS service
    this.ecsService = new ecs.FargateService(this, 'Service', {
      cluster,
      taskDefinition,
      serviceName: `${this.stackName}-service`,
      desiredCount: this.stage === 'production' ? 2 : 1,
      assignPublicIp: false,
      securityGroups: [serviceSecurityGroup],
      vpcSubnets: {
        subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS,
      },
      enableExecuteCommand: this.stage !== 'production',
    });

    // Attach the service to the target group
    this.targetGroup.addTarget(this.ecsService);

    // Create ALB listener
    const listener = this.applicationLoadBalancer.addListener('Listener', {
      port: 80,
      protocol: elbv2.ApplicationProtocol.HTTP,
      defaultTargetGroups: [this.targetGroup],
    });

    // If we have a regional certificate and hosted zone, set up HTTPS and custom domain
    // Use regional certificate for ALB (not CloudFront certificate)
    if (this.regionalCertificate && this.hostedZone && subdomain) {
      // Create API domain name
      this.apiDomainName =
        this.stage === 'production'
          ? `${subdomain}.${this.hostedZone.zoneName}`
          : `${subdomain}.${this.stage}.${this.hostedZone.zoneName}`;

      console.log(
        `Setting up HTTPS for ${this.serviceName} at ${this.apiDomainName} using regional certificate`
      );

      // Add HTTPS listener
      const httpsListener = this.applicationLoadBalancer.addListener(
        'HttpsListener',
        {
          port: 443,
          protocol: elbv2.ApplicationProtocol.HTTPS,
          certificates: [this.regionalCertificate],
          defaultTargetGroups: [this.targetGroup],
        }
      );

      // Redirect HTTP to HTTPS
      listener.addAction('RedirectToHttps', {
        action: elbv2.ListenerAction.redirect({
          protocol: elbv2.ApplicationProtocol.HTTPS,
          port: '443',
          permanent: true,
        }),
      });

      // Create Route 53 record
      new route53.ARecord(this, 'ApiDomainRecord', {
        zone: this.hostedZone,
        recordName: subdomain,
        target: route53.RecordTarget.fromAlias(
          new targets.LoadBalancerTarget(this.applicationLoadBalancer)
        ),
      });

      // Create SSM parameter for API domain
      this.createSsmParameter(
        'ApiDomainName',
        this.apiDomainName,
        `API domain name for ${this.serviceName}`
      );
    }

    // Create SSM parameters for the service
    this.createSsmParameter(
      'ServiceName',
      this.ecsService.serviceName,
      `ECS service name for ${this.serviceName}`
    );

    this.createSsmParameter(
      'LoadBalancerDnsName',
      this.applicationLoadBalancer.loadBalancerDnsName,
      `Load balancer DNS name for ${this.serviceName}`
    );

    this.createSsmParameter(
      'TargetGroupArn',
      this.targetGroup.targetGroupArn,
      `Target group ARN for ${this.serviceName}`
    );

    // Outputs
    new cdk.CfnOutput(this, `${this.serviceName}ServiceName`, {
      value: this.ecsService.serviceName,
      description: `ECS service name for ${this.serviceName}`,
    });

    new cdk.CfnOutput(this, `${this.serviceName}LoadBalancerDnsName`, {
      value: this.applicationLoadBalancer.loadBalancerDnsName,
      description: `Load balancer DNS name for ${this.serviceName}`,
    });

    if (this.apiDomainName) {
      new cdk.CfnOutput(this, `${this.serviceName}ApiDomainName`, {
        value: this.apiDomainName,
        description: `API domain name for ${this.serviceName}`,
      });
    }

    // Add tags
    cdk.Tags.of(this.ecsService).add('Service', this.serviceName);
    cdk.Tags.of(this.ecsService).add('Component', 'Backend');
    cdk.Tags.of(this.ecsService).add('Environment', this.stage);

    cdk.Tags.of(this.applicationLoadBalancer).add('Service', this.serviceName);
    cdk.Tags.of(this.applicationLoadBalancer).add('Component', 'Backend');
    cdk.Tags.of(this.applicationLoadBalancer).add('Environment', this.stage);
  }
}
