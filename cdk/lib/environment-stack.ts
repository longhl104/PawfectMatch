import * as cdk from 'aws-cdk-lib';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import * as apigateway from 'aws-cdk-lib/aws-apigateway';
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
  private readonly api: apigateway.RestApi;

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

    // Create API Gateway
    this.api = new apigateway.RestApi(this, 'PawfectMatchApi', {
      restApiName: `pawfect-match-api-${stage}`,
      description: `PawfectMatch API for ${stage} environment`,

      // CORS configuration
      defaultCorsPreflightOptions: {
        allowOrigins:
          stage === 'production'
            ? ['https://www.pawfectmatch.com']
            : [
                'https://development.pawfectmatch.com',
                'https://localhost:4200',
              ],
        allowMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS'],
        allowHeaders: ['Content-Type', 'Authorization'],
      },

      // Deployment options
      deployOptions: {
        stageName: stage,
      },
    });

    this.api.addUsagePlan('PawfectMatchUsagePlan', {
      name: `PawfectMatchUsagePlan-${stage}`,
      description: `Usage plan for PawfectMatch API in ${stage} environment`,
      apiStages: [
        {
          api: this.api,
          stage: this.api.deploymentStage,
        },
      ],
      throttle:
        stage === 'production'
          ? {
              burstLimit: 2000,
              rateLimit: 1000,
            }
          : {
              burstLimit: 200,
              rateLimit: 100,
            },
      quota:
        stage === 'production'
          ? {
              limit: 1000000,
              period: apigateway.Period.MONTH,
            }
          : {
              limit: 10000,
              period: apigateway.Period.MONTH,
            },
    });

    // Export API Gateway for other stacks to use
    this.exportValue(this.api.restApiId, {
      name: `${stage}ApiGatewayId`,
    });

    this.exportValue(this.api.root.resourceId, {
      name: `${stage}ApiGatewayRootResourceId`,
    });

    this.exportValue(this.api.url, {
      name: `${stage}ApiGatewayUrl`,
    });
  }
}
