import * as cdk from 'aws-cdk-lib';
import * as s3 from 'aws-cdk-lib/aws-s3';
import * as cloudfront from 'aws-cdk-lib/aws-cloudfront';
import * as route53 from 'aws-cdk-lib/aws-route53';
import { Construct } from 'constructs';

export interface SharedStackProps extends cdk.StackProps {}

export class SharedStack extends cdk.Stack {
  public readonly assetsBucket: s3.Bucket;
  public readonly hostedZone: route53.IHostedZone;
  public readonly distribution: cloudfront.Distribution;

  constructor(scope: Construct, id: string, props: SharedStackProps) {
    super(scope, id, props);
  }
}
