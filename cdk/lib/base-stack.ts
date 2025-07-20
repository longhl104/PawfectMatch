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
import { RemovalPolicy } from 'aws-cdk-lib';
import {
  PawfectMatchBaseStackProps,
  StageType,
  DomainUtils,
  ClientHostingConfig,
} from './utils';
import { Construct } from 'constructs';

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
  protected readonly environmentStack: cdk.Stack;
  protected readonly serviceName: string;
  protected readonly hostedZone?: route53.IHostedZone;
  protected readonly certificate?: acm.ICertificate;

  // Client hosting resources
  public clientBucket?: s3.Bucket;
  public clientDistribution?: cloudfront.Distribution;
  public clientDomainName?: string;

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
          domainNames: [this.clientDomainName]
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
}
