import * as cdk from 'aws-cdk-lib';
import * as s3 from 'aws-cdk-lib/aws-s3';
import * as cloudfront from 'aws-cdk-lib/aws-cloudfront';
import * as route53 from 'aws-cdk-lib/aws-route53';
import * as acm from 'aws-cdk-lib/aws-certificatemanager';
import * as ssm from 'aws-cdk-lib/aws-ssm';
import { Construct } from 'constructs';
import { StageType, DomainConfigManager } from './utils';
import { BaseStack } from './base-stack';

export interface SharedStackProps extends cdk.StackProps {
  stage?: StageType;
}

export class SharedStack extends cdk.Stack {
  public readonly assetsBucket?: s3.Bucket;
  public readonly hostedZone?: route53.IHostedZone;
  public readonly certificate?: acm.ICertificate;
  public readonly wildcardCertificate?: acm.Certificate;
  public readonly distribution?: cloudfront.Distribution;

  constructor(scope: Construct, id: string, props: SharedStackProps) {
    super(scope, id, props);

    const stage = props.stage;

    // Only create Route 53 resources for production or if stage is specified
    if (stage) {
      // Create or reference hosted zone
      if (stage === 'production') {
        // For production, create the main hosted zone
        this.hostedZone = new route53.HostedZone(this, 'HostedZone', {
          zoneName: DomainConfigManager.getRootDomain(stage),
          comment: `Hosted zone for PawfectMatch ${stage} environment`,
        });

        // Reference existing SSL certificate (created manually)
        // You'll need to manually create the certificate in ACM us-east-1 region
        // and update the certificate ARN in SSM parameter
        try {
          const certificateArn = ssm.StringParameter.valueFromLookup(
            this,
            '/PawfectMatch/Shared/CertificateArn'
          );
          if (certificateArn && certificateArn !== 'dummy-value-for-${Token}') {
            this.certificate = acm.Certificate.fromCertificateArn(
              this,
              'ImportedCertificate',
              certificateArn
            );
          }
        } catch (error) {
          console.warn(
            'Certificate not found in SSM, will deploy without custom domains'
          );
        }

        // Store hosted zone ID in SSM for other stacks to reference
        new ssm.StringParameter(this, 'HostedZoneId', {
          parameterName: '/PawfectMatch/Shared/HostedZoneId',
          stringValue: this.hostedZone.hostedZoneId,
          description: 'Hosted Zone ID for PawfectMatch domain',
        });

        // Store domain name in SSM
        new ssm.StringParameter(this, 'DomainName', {
          parameterName: '/PawfectMatch/Shared/DomainName',
          stringValue: DomainConfigManager.getRootDomain(stage),
          description: 'Root domain name for PawfectMatch',
        });

        // Store certificate ARN in SSM
        if (this.certificate) {
          new ssm.StringParameter(this, 'CertificateArn', {
            parameterName: '/PawfectMatch/Shared/CertificateArn',
            stringValue: this.certificate.certificateArn,
            description: 'SSL Certificate ARN for PawfectMatch domain',
          });
        }
      } else {
        // For other environments, reference the production hosted zone
        // You'll need to manually provide the hosted zone ID from production
        const hostedZoneId = ssm.StringParameter.valueFromLookup(
          this,
          '/PawfectMatch/Shared/HostedZoneId'
        );

        this.hostedZone = route53.HostedZone.fromHostedZoneAttributes(
          this,
          'ImportedHostedZone',
          {
            hostedZoneId: hostedZoneId,
            zoneName: 'pawfectmatchnow.com', // Always reference the root domain
          }
        );

        // Create certificate for the stage subdomain in us-east-1 for CloudFront
        try {
          const certificateArn = ssm.StringParameter.valueFromLookup(
            this,
            `/PawfectMatch/${BaseStack.getCapitalizedStage(
              stage
            )}/Common/CertificateArn`
          );

          console.log(
            `Certificate ARN for ${stage} environment: ${certificateArn}`
          );
          if (certificateArn && certificateArn !== 'dummy-value-for-${Token}') {
            this.certificate = acm.Certificate.fromCertificateArn(
              this,
              'ImportedCertificate',
              certificateArn
            );
          }
        } catch (error) {
          console.warn(
            `Certificate not found for ${stage} environment, will deploy without custom domains`
          );
        }
      }
    }
  }
}
