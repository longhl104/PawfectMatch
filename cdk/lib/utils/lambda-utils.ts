import * as lambda from 'aws-cdk-lib/aws-lambda';
import * as iam from 'aws-cdk-lib/aws-iam';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import { Duration } from 'aws-cdk-lib';
import { Construct } from 'constructs';
import * as path from 'path';
import { StageType } from './stack-props';

export interface LambdaFunctionConfig {
  functionName: string;
  handler?: string;
  codePath?: string;
  timeout?: Duration;
  environment?: { [key: string]: string };
  memorySize?: number;
  description: string;
  vpc?: ec2.IVpc;
  vpcSubnets?: ec2.SubnetSelection;
  securityGroups?: ec2.ISecurityGroup[];
}

export class LambdaUtils {
  /**
   * Creates a Lambda function with common configuration
   */
  static createFunction(
    scope: Construct,
    id: string,
    serviceGroup: string,
    stage: StageType,
    config: LambdaFunctionConfig
  ): lambda.Function {
    const lambdaFunction = new lambda.Function(scope, id, {
      functionName: `pawfect-match-${serviceGroup}-${config.functionName}-${stage}`,
      runtime: lambda.Runtime.DOTNET_8,
      handler: `${config.functionName}::${config.functionName}.Function::FunctionHandler`,
      code: lambda.Code.fromAsset(
        path.join(
          __dirname,
          `../../../${serviceGroup}/Lambdas/${config.functionName}/src/${config.functionName}/bin/Release/net8.0/publish`
        )
      ),
      timeout: config.timeout ?? Duration.seconds(30),
      memorySize: config.memorySize ?? 128,
      environment: config.environment ?? {},
      description: config.description,
      vpc: config.vpc,
      vpcSubnets: config.vpcSubnets,
      securityGroups: config.securityGroups,
    });

    return lambdaFunction;
  }
}
