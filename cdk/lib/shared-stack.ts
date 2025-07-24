import * as cdk from 'aws-cdk-lib';
import * as s3 from 'aws-cdk-lib/aws-s3';
import * as cloudfront from 'aws-cdk-lib/aws-cloudfront';
import * as route53 from 'aws-cdk-lib/aws-route53';
import * as acm from 'aws-cdk-lib/aws-certificatemanager';
import * as ssm from 'aws-cdk-lib/aws-ssm';
import * as ses from 'aws-cdk-lib/aws-ses';
import * as iam from 'aws-cdk-lib/aws-iam';
import { Construct } from 'constructs';
import { StageType } from './utils';
import { BaseStack } from './base-stack';

export interface SharedStackProps extends cdk.StackProps {
  stage?: StageType;
}

export class SharedStack extends cdk.Stack {
  public readonly assetsBucket?: s3.Bucket;
  public readonly hostedZone?: route53.IHostedZone;
  public readonly certificate?: acm.ICertificate;
  public readonly distribution?: cloudfront.Distribution;
  public readonly regionalCertificate?: acm.ICertificate;
  public emailIdentity?: ses.EmailIdentity;
  public sesConfigurationSet?: ses.ConfigurationSet;

  constructor(scope: Construct, id: string, props: SharedStackProps) {
    super(scope, id, props);

    const stage = props.stage;

    // Only create Route 53 resources for production or if stage is specified
    if (stage) {
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

      // Create regional certificate for ALB (Application Load Balancer) usage
      // This is different from the CloudFront certificate which must be in us-east-1
      try {
        const regionalCertificateArn = ssm.StringParameter.valueFromLookup(
          this,
          `/PawfectMatch/${BaseStack.getCapitalizedStage(
            stage
          )}/Common/RegionalCertificateArn`
        );

        console.log(
          `Regional Certificate ARN for ${stage} environment: ${regionalCertificateArn}`
        );
        if (
          regionalCertificateArn &&
          regionalCertificateArn !== 'dummy-value-for-${Token}'
        ) {
          this.regionalCertificate = acm.Certificate.fromCertificateArn(
            this,
            'ImportedRegionalCertificate',
            regionalCertificateArn
          );
        }
      } catch (error) {
        console.warn(
          `Regional certificate not found for ${stage} environment, ALB will use HTTP only`
        );
      }

      // Set up SES (Simple Email Service) for sending emails
      this.setupEmailService(stage);
    }
  }

  private setupEmailService(stage: StageType): void {
    // Create SES Configuration Set for email tracking and management
    this.sesConfigurationSet = new ses.ConfigurationSet(
      this,
      'EmailConfigurationSet',
      {
        configurationSetName: `pawfectmatch-${stage}`,
        sendingEnabled: true,
      }
    );

    // Create email identity for the domain
    const domainName = stage === 'production'
      ? 'pawfectmatchnow.com'
      : `${stage}.pawfectmatchnow.com`;

    this.emailIdentity = new ses.EmailIdentity(
      this,
      'EmailIdentity',
      {
        identity: ses.Identity.domain(domainName),
        configurationSet: this.sesConfigurationSet,
        mailFromDomain: `mail.${domainName}`,
        // Enable DKIM signing for better deliverability
        dkimSigning: true,
      }
    );

    // Store SES configuration in SSM for applications to use
    new ssm.StringParameter(this, 'SESConfigurationSetName', {
      parameterName: `/PawfectMatch/${BaseStack.getCapitalizedStage(stage)}/Common/SESConfigurationSetName`,
      stringValue: this.sesConfigurationSet.configurationSetName,
      description: `SES Configuration Set name for ${stage} environment`,
    });

    new ssm.StringParameter(this, 'SESFromDomain', {
      parameterName: `/PawfectMatch/${BaseStack.getCapitalizedStage(stage)}/Common/SESFromDomain`,
      stringValue: domainName,
      description: `SES verified domain for ${stage} environment`,
    });

    // Create predefined email addresses for different purposes
    const emailAddresses = {
      noreply: `noreply@${domainName}`,
      support: `support@${domainName}`,
      notifications: `notifications@${domainName}`,
      welcome: `welcome@${domainName}`,
      admin: `admin@${domainName}`,
    };

    // Store email addresses in SSM
    Object.entries(emailAddresses).forEach(([key, email]) => {
      new ssm.StringParameter(this, `SESEmail${key.charAt(0).toUpperCase() + key.slice(1)}`, {
        parameterName: `/PawfectMatch/${BaseStack.getCapitalizedStage(stage)}/Common/Email${key.charAt(0).toUpperCase() + key.slice(1)}`,
        stringValue: email,
        description: `${key} email address for ${stage} environment`,
      });
    });

    // Output the DNS records needed for domain verification
    new cdk.CfnOutput(this, 'SESVerificationInstructions', {
      value: `Add the following DNS records to verify domain ${domainName} in SES`,
      description: 'SES Domain Verification Instructions',
    });

    new cdk.CfnOutput(this, 'SESDomainIdentity', {
      value: this.emailIdentity.emailIdentityName,
      description: 'SES Email Identity Name',
    });
  }
}
