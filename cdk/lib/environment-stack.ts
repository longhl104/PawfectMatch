import * as cdk from 'aws-cdk-lib';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import * as ecs from 'aws-cdk-lib/aws-ecs';
import * as logs from 'aws-cdk-lib/aws-logs';
import * as iam from 'aws-cdk-lib/aws-iam';
import * as ecr from 'aws-cdk-lib/aws-ecr';
import * as rds from 'aws-cdk-lib/aws-rds';
import * as ssm from 'aws-cdk-lib/aws-ssm';
import * as lambda from 'aws-cdk-lib/aws-lambda';
import * as custom from 'aws-cdk-lib/custom-resources';
import { Construct } from 'constructs';
import { SharedStack } from './shared-stack';
import { StageType } from './utils';
import { BaseStack } from './base-stack';
import { LambdaUtils } from './utils/lambda-utils';

export interface EnvironmentStackProps extends cdk.StackProps {
  stage: StageType; // 'development' or 'production'
  sharedStack: SharedStack; // Reference to the shared stack
  // Add other properties as needed
}

export class EnvironmentStack extends cdk.Stack {
  public readonly vpc: ec2.Vpc;
  public readonly ecsCluster: ecs.Cluster;
  public readonly taskExecutionRole: iam.Role;
  public readonly taskRole: iam.Role;
  public readonly logGroup: logs.LogGroup;

  // ECR repositories for backend services
  public readonly identityRepository: ecr.IRepository;
  public readonly matcherRepository: ecr.IRepository;
  public readonly shelterHubRepository: ecr.IRepository;

  // Database-related properties
  public database: rds.DatabaseInstance;
  public databaseSecurityGroup: ec2.SecurityGroup;
  public postgisInstallerFunction: lambda.Function;
  public postgisInstallerCustomResource: custom.AwsCustomResource;

  stage: StageType;

  constructor(scope: Construct, id: string, props: EnvironmentStackProps) {
    super(scope, id, props);

    const { stage } = props;

    this.stage = stage;

    // Create VPC with cost optimization - single NAT gateway even in production
    this.vpc = new ec2.Vpc(this, 'PawfectMatchVpc', {
      maxAzs: 2,
      natGateways: 1, // Single NAT Gateway to save $45/month per additional gateway
      subnetConfiguration: [
        {
          cidrMask: 24,
          name: 'Public',
          subnetType: ec2.SubnetType.PUBLIC,
        },
        {
          cidrMask: 24,
          name: 'Private',
          subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS,
        },
        {
          cidrMask: 28,
          name: 'Database',
          subnetType: ec2.SubnetType.PRIVATE_ISOLATED,
        },
      ],
    });

    // Create ECS Cluster with cost optimization
    this.ecsCluster = new ecs.Cluster(this, 'PawfectMatchCluster', {
      vpc: this.vpc,
      clusterName: `pawfectmatch-${stage}-cluster`,
      containerInsightsV2: ecs.ContainerInsights.DISABLED,
    });

    // Create CloudWatch Log Group with cost optimization
    this.logGroup = new logs.LogGroup(this, 'PawfectMatchLogGroup', {
      logGroupName: `/aws/ecs/pawfectmatch-${stage}`,
      retention: logs.RetentionDays.THREE_DAYS, // Minimal retention to save on log storage
      removalPolicy: cdk.RemovalPolicy.DESTROY, // Allow deletion to avoid retention costs
    });

    // Create Task Execution Role
    this.taskExecutionRole = new iam.Role(this, 'TaskExecutionRole', {
      assumedBy: new iam.ServicePrincipal('ecs-tasks.amazonaws.com'),
      managedPolicies: [
        iam.ManagedPolicy.fromAwsManagedPolicyName(
          'service-role/AmazonECSTaskExecutionRolePolicy'
        ),
      ],
      inlinePolicies: {
        ECRAccess: new iam.PolicyDocument({
          statements: [
            new iam.PolicyStatement({
              effect: iam.Effect.ALLOW,
              actions: [
                'ecr:BatchCheckLayerAvailability',
                'ecr:GetDownloadUrlForLayer',
                'ecr:BatchGetImage',
              ],
              resources: ['*'],
            }),
            new iam.PolicyStatement({
              effect: iam.Effect.ALLOW,
              actions: ['ecr:GetAuthorizationToken'],
              resources: ['*'],
            }),
          ],
        }),
        CloudWatchLogs: new iam.PolicyDocument({
          statements: [
            new iam.PolicyStatement({
              effect: iam.Effect.ALLOW,
              actions: ['logs:CreateLogStream', 'logs:PutLogEvents'],
              resources: [this.logGroup.logGroupArn],
            }),
          ],
        }),
      },
    });

    // Create Task Role (for application permissions)
    this.taskRole = new iam.Role(this, 'TaskRole', {
      assumedBy: new iam.ServicePrincipal('ecs-tasks.amazonaws.com'),
      inlinePolicies: {
        PawfectMatchServicePolicy: new iam.PolicyDocument({
          statements: [
            // DynamoDB access
            new iam.PolicyStatement({
              effect: iam.Effect.ALLOW,
              actions: [
                'dynamodb:GetItem',
                'dynamodb:PutItem',
                'dynamodb:UpdateItem',
                'dynamodb:DeleteItem',
                'dynamodb:Query',
                'dynamodb:Scan',
                'dynamodb:BatchGetItem',
                'dynamodb:BatchWriteItem',
              ],
              resources: [
                `arn:aws:dynamodb:${this.region}:${this.account}:table/pawfectmatch-${stage}-*`,
              ],
            }),
            // Systems Manager Parameter Store access
            new iam.PolicyStatement({
              effect: iam.Effect.ALLOW,
              actions: [
                'ssm:GetParameter',
                'ssm:GetParameters',
                'ssm:GetParametersByPath',
                'ssm:PutParameter',
                'ssm:DeleteParameter',
                'ssm:AddTagsToResource',
                'ssm:RemoveTagsFromResource',
              ],
              resources: [
                `arn:aws:ssm:${this.region}:${this.account}:parameter/PawfectMatch/*`,
              ],
            }),
            // Cognito access
            new iam.PolicyStatement({
              effect: iam.Effect.ALLOW,
              actions: [
                'cognito-idp:AdminCreateUser',
                'cognito-idp:AdminDeleteUser',
                'cognito-idp:AdminGetUser',
                'cognito-idp:AdminUpdateUserAttributes',
                'cognito-idp:ListUsers',
                'cognito-idp:AdminInitiateAuth',
                'cognito-idp:AdminConfirmSignUp',
                'cognito-idp:AdminSetUserPassword',
              ],
              resources: [
                `arn:aws:cognito-idp:${this.region}:${this.account}:userpool/*`,
              ],
            }),
            // S3 access (for media uploads and data protection keys)
            new iam.PolicyStatement({
              effect: iam.Effect.ALLOW,
              actions: ['s3:GetObject', 's3:PutObject', 's3:DeleteObject'],
              resources: [`arn:aws:s3:::pawfectmatch-${stage}-*/*`],
            }),
            // Secrets Manager access for database credentials
            new iam.PolicyStatement({
              effect: iam.Effect.ALLOW,
              actions: [
                'secretsmanager:GetSecretValue',
                'secretsmanager:DescribeSecret',
              ],
              resources: [
                `arn:aws:secretsmanager:${this.region}:${this.account}:secret:pawfectmatch-${stage}-db-credentials-*`,
              ],
            }),
            // KMS access for Data Protection
            new iam.PolicyStatement({
              effect: iam.Effect.ALLOW,
              actions: [
                'kms:Decrypt',
                'kms:Encrypt',
                'kms:GenerateDataKey',
                'kms:DescribeKey',
              ],
              resources: [`arn:aws:kms:${this.region}:${this.account}:key/*`],
            }),
          ],
        }),
      },
    });

    // Import existing ECR repositories for backend services
    const services = ['identity', 'matcher', 'shelterhub'];
    const repositories: { [key: string]: ecr.IRepository } = {};

    services.forEach((service) => {
      const repositoryName = `pawfectmatch-${service}-${stage}`;
      const repository = ecr.Repository.fromRepositoryName(
        this,
        `${service}EcrRepository`,
        repositoryName
      );
      repositories[service] = repository;
    });

    this.identityRepository = repositories['identity'];
    this.matcherRepository = repositories['matcher'];
    this.shelterHubRepository = repositories['shelterhub'];

    // Set up shared PostgreSQL database
    this.setupDatabase(stage);

    // Create PostGIS installer Lambda function
    this.setupPostgisInstaller(stage);
  }

  private setupDatabase(stage: StageType): void {
    // Create security group for the database
    this.databaseSecurityGroup = new ec2.SecurityGroup(
      this,
      'DatabaseSecurityGroup',
      {
        vpc: this.vpc,
        description: 'Security group for PostgreSQL database',
        allowAllOutbound: false,
      }
    );

    // Allow inbound connections on PostgreSQL port from within VPC
    this.databaseSecurityGroup.addIngressRule(
      ec2.Peer.ipv4(this.vpc.vpcCidrBlock),
      ec2.Port.tcp(5432),
      'PostgreSQL access from VPC'
    );

    // Create subnet group for the database
    const dbSubnetGroup = new rds.SubnetGroup(this, 'DatabaseSubnetGroup', {
      vpc: this.vpc,
      description: 'Subnet group for PostgreSQL database',
      vpcSubnets: {
        subnetType: ec2.SubnetType.PRIVATE_ISOLATED,
      },
    });

    // Create the PostgreSQL database instance
    this.database = new rds.DatabaseInstance(this, 'PostgreSQLDatabase', {
      engine: rds.DatabaseInstanceEngine.postgres({
        version: rds.PostgresEngineVersion.VER_17_5,
      }),
      instanceType: ec2.InstanceType.of(
        ec2.InstanceClass.T3,
        ec2.InstanceSize.MICRO
      ),
      vpc: this.vpc,
      subnetGroup: dbSubnetGroup,
      securityGroups: [this.databaseSecurityGroup],
      databaseName: 'pawfectmatch',
      credentials: rds.Credentials.fromGeneratedSecret('dbadmin', {
        secretName: `pawfectmatch-${stage}-db-credentials`,
      }),
      allocatedStorage: 20,
      storageType: rds.StorageType.GP2,
      deleteAutomatedBackups: stage !== 'production',
      backupRetention:
        stage === 'production' ? cdk.Duration.days(7) : cdk.Duration.days(1),
      deletionProtection: stage === 'production',
      enablePerformanceInsights: false, // Save costs
      monitoringInterval: cdk.Duration.seconds(0), // Disable enhanced monitoring to save costs
      autoMinorVersionUpgrade: true,
      allowMajorVersionUpgrade: false,
      parameterGroup: rds.ParameterGroup.fromParameterGroupName(
        this,
        'DefaultPostgreSQLParameterGroup',
        'default.postgres17'
      ),
    });

    // Store database connection information in SSM Parameter Store
    new ssm.StringParameter(this, 'DatabaseHost', {
      parameterName: `/PawfectMatch/${BaseStack.getCapitalizedStage(
        stage
      )}/Common/Database/Host`,
      stringValue: this.database.instanceEndpoint.hostname,
      description: `PostgreSQL database host for ${stage} environment`,
    });

    new ssm.StringParameter(this, 'DatabasePort', {
      parameterName: `/PawfectMatch/${BaseStack.getCapitalizedStage(
        stage
      )}/Common/Database/Port`,
      stringValue: this.database.instanceEndpoint.port.toString(),
      description: `PostgreSQL database port for ${stage} environment`,
    });

    new ssm.StringParameter(this, 'DatabaseName', {
      parameterName: `/PawfectMatch/${BaseStack.getCapitalizedStage(
        stage
      )}/Common/Database/Name`,
      stringValue: 'pawfectmatch',
      description: `PostgreSQL database name for ${stage} environment`,
    });

    new ssm.StringParameter(this, 'DatabaseSecretArn', {
      parameterName: `/PawfectMatch/${BaseStack.getCapitalizedStage(
        stage
      )}/Common/Database/SecretArn`,
      stringValue: this.database.secret!.secretArn,
      description: `PostgreSQL database credentials secret ARN for ${stage} environment`,
    });

    // Store network information for other stacks to use
    new ssm.StringParameter(this, 'DatabaseSecurityGroupId', {
      parameterName: `/PawfectMatch/${BaseStack.getCapitalizedStage(
        stage
      )}/Common/DatabaseSecurityGroupId`,
      stringValue: this.databaseSecurityGroup.securityGroupId,
      description: `Database security group ID for ${stage} environment`,
    });

    // Output important information
    new cdk.CfnOutput(this, 'DatabaseEndpoint', {
      value: this.database.instanceEndpoint.hostname,
      description: 'PostgreSQL database endpoint',
    });

    new cdk.CfnOutput(this, 'DatabaseSecretName', {
      value: this.database.secret!.secretName,
      description: 'Database credentials secret name',
    });

    new cdk.CfnOutput(this, `${this.stage}DatabaseSecurityGroupId`, {
      value: this.databaseSecurityGroup.securityGroupId,
      description: 'Database security group ID',
    });
  }

  private setupPostgisInstaller(stage: StageType): void {
    // Create security group for the Lambda function first
    const lambdaSecurityGroup = new ec2.SecurityGroup(
      this,
      'PostgisInstallerSecurityGroup',
      {
        vpc: this.vpc,
        description: 'Security group for PostGIS installer Lambda',
        allowAllOutbound: true,
      }
    );

    // Allow the Lambda function to connect to the database
    this.databaseSecurityGroup.addIngressRule(
      lambdaSecurityGroup,
      ec2.Port.tcp(5432),
      'PostGIS installer Lambda access'
    );

    // Create the PostGIS installer Lambda function with VPC configuration
    this.postgisInstallerFunction = LambdaUtils.createFunction(
      this,
      'PostgisInstaller',
      'Environment',
      stage,
      {
        functionName: 'InstallPostgis',
        description: 'Installs PostGIS extension in PostgreSQL database',
        timeout: cdk.Duration.minutes(1),
        memorySize: 256,
        environment: {
          STAGE: stage,
        },
        vpc: this.vpc,
        vpcSubnets: {
          subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS,
        },
        securityGroups: [lambdaSecurityGroup],
      }
    );

    // Grant the Lambda function permission to access the database secret
    this.postgisInstallerFunction.addToRolePolicy(
      new iam.PolicyStatement({
        effect: iam.Effect.ALLOW,
        actions: [
          'secretsmanager:GetSecretValue',
          'secretsmanager:DescribeSecret',
        ],
        resources: [
          `arn:aws:secretsmanager:${this.region}:${this.account}:secret:pawfectmatch-${stage}-db-credentials-*`,
        ],
      })
    );

    // Grant VPC permissions to the Lambda function
    this.postgisInstallerFunction.addToRolePolicy(
      new iam.PolicyStatement({
        effect: iam.Effect.ALLOW,
        actions: [
          'ec2:CreateNetworkInterface',
          'ec2:DescribeNetworkInterfaces',
          'ec2:DeleteNetworkInterface',
          'ec2:AttachNetworkInterface',
          'ec2:DetachNetworkInterface',
        ],
        resources: ['*'],
      })
    );

    // Store the Lambda function ARN in SSM Parameter Store for easy access
    new ssm.StringParameter(this, 'PostgisInstallerArn', {
      parameterName: `/PawfectMatch/${BaseStack.getCapitalizedStage(
        stage
      )}/Lambda/PostgisInstallerArn`,
      stringValue: this.postgisInstallerFunction.functionArn,
      description: `PostGIS installer Lambda function ARN for ${stage} environment`,
    });

    // Output the Lambda function ARN
    new cdk.CfnOutput(this, 'PostgisInstallerFunctionArn', {
      value: this.postgisInstallerFunction.functionArn,
      description: 'PostGIS installer Lambda function ARN',
    });

    // Create custom resource to automatically invoke PostGIS installer after deployment
    this.postgisInstallerCustomResource = new custom.AwsCustomResource(
      this,
      'PostgisInstallerCustomResource',
      {
        onCreate: {
          service: 'Lambda',
          action: 'invoke',
          parameters: {
            FunctionName: this.postgisInstallerFunction.functionName,
            Payload: JSON.stringify({ Stage: stage }),
          },
          physicalResourceId: custom.PhysicalResourceId.of(
            `postgis-installer-${stage}-${Date.now()}`
          ),
        },
        onUpdate: {
          service: 'Lambda',
          action: 'invoke',
          parameters: {
            FunctionName: this.postgisInstallerFunction.functionName,
            Payload: JSON.stringify({ Stage: stage }),
          },
          physicalResourceId: custom.PhysicalResourceId.of(
            `postgis-installer-${stage}-${Date.now()}`
          ),
        },
        policy: custom.AwsCustomResourcePolicy.fromStatements([
          new iam.PolicyStatement({
            effect: iam.Effect.ALLOW,
            actions: ['lambda:InvokeFunction'],
            resources: [this.postgisInstallerFunction.functionArn],
          }),
        ]),
        timeout: cdk.Duration.minutes(5), // Give enough time for the Lambda to complete
        logRetention: logs.RetentionDays.ONE_WEEK,
      }
    );

    // Ensure the custom resource runs after the database is ready
    this.postgisInstallerCustomResource.node.addDependency(this.database);

    // Store the custom resource reference for monitoring
    new ssm.StringParameter(this, 'PostgisInstallerCustomResourceId', {
      parameterName: `/PawfectMatch/${BaseStack.getCapitalizedStage(
        stage
      )}/Lambda/PostgisInstallerCustomResourceId`,
      stringValue: this.postgisInstallerCustomResource.node.id,
      description: `PostGIS installer custom resource ID for ${stage} environment`,
    });

    // Output the custom resource information
    new cdk.CfnOutput(
      this,
      `${BaseStack.getCapitalizedStage(stage)}PostgisInstallerCustomResourceId`,
      {
        value: this.postgisInstallerCustomResource.node.id,
        description: 'PostGIS installer custom resource ID',
      }
    );
  }
}
