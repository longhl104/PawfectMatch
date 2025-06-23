import * as cdk from 'aws-cdk-lib';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import * as rds from 'aws-cdk-lib/aws-rds';
import * as iam from 'aws-cdk-lib/aws-iam';
import * as lambda from 'aws-cdk-lib/aws-lambda';
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
  public readonly database: rds.DatabaseInstance;

  constructor(scope: Construct, id: string, props: EnvironmentStackProps) {
    super(scope, id, props);

    const { stage, sharedStack } = props;

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

    // Create security group for database
    const dbSecurityGroup = new ec2.SecurityGroup(
      this,
      'DatabaseSecurityGroup',
      {
        vpc: this.vpc,
        description: 'Security group for PostgreSQL database',
        allowAllOutbound: false,
      }
    );

    // Allow inbound connections from VPC on PostgreSQL port
    dbSecurityGroup.addIngressRule(
      ec2.Peer.ipv4(this.vpc.vpcCidrBlock),
      ec2.Port.tcp(5432),
      'Allow PostgreSQL connections from VPC'
    );

    // Create DB subnet group
    const dbSubnetGroup = new rds.SubnetGroup(this, 'DatabaseSubnetGroup', {
      vpc: this.vpc,
      description: 'Subnet group for PostgreSQL database',
      vpcSubnets: {
        subnetType: ec2.SubnetType.PRIVATE_ISOLATED,
      },
    });

    // Create PostgreSQL database with PostGIS extension
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
      securityGroups: [dbSecurityGroup],
      databaseName: 'pawfectmatch',
      credentials: rds.Credentials.fromGeneratedSecret('dbadmin', {
        secretName: `pawfectmatch-db-credentials-${stage}`,
      }),
      backupRetention:
        stage === 'production' ? cdk.Duration.days(7) : cdk.Duration.days(1),
      deletionProtection: stage === 'production',
      multiAz: stage === 'production',
      storageEncrypted: true,
      allocatedStorage: stage === 'production' ? 100 : 20,
      maxAllocatedStorage: stage === 'production' ? 1000 : 100,
      parameterGroup: new rds.ParameterGroup(this, 'DatabaseParameterGroup', {
        engine: rds.DatabaseInstanceEngine.postgres({
          version: rds.PostgresEngineVersion.VER_17_5,
        }),
        parameters: {
          log_statement: stage === 'production' ? 'none' : 'all',
        },
      }),
    });

    // Output the database endpoint
    new cdk.CfnOutput(this, 'DatabaseEndpoint', {
      value: this.database.instanceEndpoint.hostname,
      description: 'PostgreSQL database endpoint',
    });

    // Output the VPC ID
    new cdk.CfnOutput(this, 'VpcId', {
      value: this.vpc.vpcId,
      description: 'VPC ID',
    });

    // Create Lambda execution role
    const lambdaRole = new iam.Role(this, 'PostGISLambdaRole', {
      assumedBy: new iam.ServicePrincipal('lambda.amazonaws.com'),
      managedPolicies: [
        iam.ManagedPolicy.fromAwsManagedPolicyName(
          'service-role/AWSLambdaVPCAccessExecutionRole'
        ),
      ],
    });

    // Grant Lambda access to read secrets
    this.database.secret?.grantRead(lambdaRole);

    // Create Lambda function for PostGIS installation
    new lambda.Function(this, 'PostGISInstaller', {
      runtime: lambda.Runtime.PYTHON_3_9,
      handler: 'index.handler',
      role: lambdaRole,
      vpc: this.vpc,
      vpcSubnets: {
        subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS,
      },
      securityGroups: [dbSecurityGroup],
      code: lambda.Code.fromInline(`
import json
import psycopg2
import boto3

def handler(event, context):
    # Get database credentials from Secrets Manager
    secrets_client = boto3.client('secretsmanager')
    secret_value = secrets_client.get_secret_value(SecretId='${this.database.secret?.secretArn}')
    secret = json.loads(secret_value['SecretString'])

    try:
        # Connect to database
        conn = psycopg2.connect(
            host='${this.database.instanceEndpoint.hostname}',
            database='pawfectmatch',
            user=secret['username'],
            password=secret['password']
        )

        cursor = conn.cursor()

        # Install PostGIS extensions
        cursor.execute("CREATE EXTENSION IF NOT EXISTS postgis;")
        cursor.execute("CREATE EXTENSION IF NOT EXISTS postgis_topology;")

        conn.commit()
        cursor.close()
        conn.close()

        return {
            'statusCode': 200,
            'body': json.dumps('PostGIS installed successfully')
        }
    except Exception as e:
        return {
            'statusCode': 500,
            'body': json.dumps(f'Error: {str(e)}')
        }
      `),
      timeout: cdk.Duration.minutes(5),
    });
  }
}
