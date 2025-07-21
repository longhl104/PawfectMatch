import * as cdk from 'aws-cdk-lib';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import * as ecs from 'aws-cdk-lib/aws-ecs';
import * as logs from 'aws-cdk-lib/aws-logs';
import * as iam from 'aws-cdk-lib/aws-iam';
import * as ecr from 'aws-cdk-lib/aws-ecr';
import { Construct } from 'constructs';
import { SharedStack } from './shared-stack';
import { StageType } from './utils';

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

  constructor(scope: Construct, id: string, props: EnvironmentStackProps) {
    super(scope, id, props);

    const { stage } = props;

    // Create VPC
    this.vpc = new ec2.Vpc(this, 'PawfectMatchVpc', {
      maxAzs: 2,
      natGateways: stage === 'production' ? 2 : 1,
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

    // Create ECS Cluster
    this.ecsCluster = new ecs.Cluster(this, 'PawfectMatchCluster', {
      vpc: this.vpc,
      clusterName: `pawfectmatch-${stage}-cluster`,
      containerInsights: stage === 'production',
    });

    // Create CloudWatch Log Group
    this.logGroup = new logs.LogGroup(this, 'PawfectMatchLogGroup', {
      logGroupName: `/aws/ecs/pawfectmatch-${stage}`,
      retention:
        stage === 'production'
          ? logs.RetentionDays.ONE_MONTH
          : logs.RetentionDays.ONE_WEEK,
      removalPolicy:
        stage === 'production'
          ? cdk.RemovalPolicy.RETAIN
          : cdk.RemovalPolicy.DESTROY,
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
            // S3 access (for media uploads)
            new iam.PolicyStatement({
              effect: iam.Effect.ALLOW,
              actions: ['s3:GetObject', 's3:PutObject', 's3:DeleteObject'],
              resources: [`arn:aws:s3:::pawfectmatch-${stage}-*/*`],
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
  }
}
