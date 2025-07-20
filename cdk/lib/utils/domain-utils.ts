import * as cdk from 'aws-cdk-lib';
import * as route53 from 'aws-cdk-lib/aws-route53';
import * as route53Targets from 'aws-cdk-lib/aws-route53-targets';
import * as cloudfront from 'aws-cdk-lib/aws-cloudfront';
import * as apigateway from 'aws-cdk-lib/aws-apigateway';
import * as acm from 'aws-cdk-lib/aws-certificatemanager';
import { Construct } from 'constructs';
import { DomainConfig } from './domain-config';

export interface DomainSetupResult {
  domainName: apigateway.DomainName;
  aRecord: route53.ARecord;
}

export class DomainUtils {
  /**
   * Set up custom domain for API Gateway
   */
  static setupApiGatewayDomain(
    scope: Construct,
    id: string,
    api: apigateway.RestApi,
    domainConfig: DomainConfig,
    certificate: acm.ICertificate,
    hostedZone: route53.IHostedZone
  ): DomainSetupResult {
    // Create custom domain name for API Gateway
    const domainName = new apigateway.DomainName(scope, `${id}-Domain`, {
      domainName: domainConfig.domainName,
      certificate: certificate,
      endpointType: apigateway.EndpointType.REGIONAL,
      securityPolicy: apigateway.SecurityPolicy.TLS_1_2,
    });

    // Create base path mapping
    new apigateway.BasePathMapping(scope, `${id}-BasePathMapping`, {
      domainName: domainName,
      restApi: api,
      basePath: '', // Root path
    });

    // Create Route 53 A record
    const aRecord = new route53.ARecord(scope, `${id}-ARecord`, {
      zone: hostedZone,
      recordName: domainConfig.domainName,
      target: route53.RecordTarget.fromAlias(
        new route53Targets.ApiGatewayDomain(domainName)
      ),
      ttl: cdk.Duration.minutes(5),
    });

    return { domainName, aRecord };
  }

  /**
   * Set up custom domain for CloudFront distribution (for SPAs)
   */
  static setupCloudFrontDomain(
    scope: Construct,
    id: string,
    distribution: cloudfront.Distribution,
    domainConfig: DomainConfig,
    hostedZone: route53.IHostedZone
  ): route53.ARecord {
    // Create Route 53 A record for CloudFront
    return new route53.ARecord(scope, `${id}-ARecord`, {
      zone: hostedZone,
      recordName: domainConfig.domainName,
      target: route53.RecordTarget.fromAlias(
        new route53Targets.CloudFrontTarget(distribution)
      ),
      ttl: cdk.Duration.minutes(5),
    });
  }

  /**
   * Create CNAME record for subdomain
   */
  static createCnameRecord(
    scope: Construct,
    id: string,
    hostedZone: route53.IHostedZone,
    recordName: string,
    domainName: string
  ): route53.CnameRecord {
    return new route53.CnameRecord(scope, `${id}-CNAME`, {
      zone: hostedZone,
      recordName: recordName,
      domainName: domainName,
      ttl: cdk.Duration.minutes(5),
    });
  }
}
