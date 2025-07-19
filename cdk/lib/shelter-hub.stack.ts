import { Construct } from 'constructs';
import { BaseStack } from './base-stack';
import { PawfectMatchBaseStackProps } from './utils';
import * as dynamodb from 'aws-cdk-lib/aws-dynamodb';
import * as s3 from 'aws-cdk-lib/aws-s3';
import { RemovalPolicy, Duration } from 'aws-cdk-lib';

export interface ShelterHubStackProps extends PawfectMatchBaseStackProps {}

export class ShelterHubStack extends BaseStack {
  public readonly shelterAdminsTable: dynamodb.Table;
  public readonly sheltersTable: dynamodb.Table;
  public readonly petMediaBucket: s3.Bucket;

  constructor(scope: Construct, id: string, props: ShelterHubStackProps) {
    super(scope, id, props);

    // Create ShelterAdmins DynamoDB Table
    this.shelterAdminsTable = this.createDynamoDbTable({
      tableName: 'shelter-admins',
      partitionKey: {
        name: 'UserId',
        type: dynamodb.AttributeType.STRING,
      },
      description: 'Table for storing shelter admin information',
    });

    // Create Shelters DynamoDB Table
    this.sheltersTable = this.createDynamoDbTable({
      tableName: 'shelters',
      partitionKey: {
        name: 'ShelterId',
        type: dynamodb.AttributeType.STRING,
      },
      description: 'Table for storing shelter information',
    });

    // Create Pets DynamoDB Table
    this.createDynamoDbTable({
      tableName: 'pets',
      partitionKey: {
        name: 'PetId',
        type: dynamodb.AttributeType.STRING,
      },
      description: 'Table for storing pet information',
      globalSecondaryIndexes: [
        {
          indexName: 'ShelterIdCreatedAtIndex',
          partitionKey: {
            name: 'ShelterId',
            type: dynamodb.AttributeType.STRING,
          },
          sortKey: {
            name: 'CreatedAt',
            type: dynamodb.AttributeType.STRING,
          },
          nonKeyAttributes: [
            'Name',
            'Species',
            'Breed',
            'DateOfBirth',
            'Gender',
            'Status',
            'MainImageFileExtension',
            'AdoptionFee'
          ],
        },
      ],
    });

    // Create S3 Bucket for Pet Media (images and videos)
    this.petMediaBucket = new s3.Bucket(this, `${this.stackName}-pet-media`, {
      bucketName: `${this.stackName}-pet-media`.toLowerCase(),
      versioned: true,
      removalPolicy:
        this.environment === 'production'
          ? RemovalPolicy.RETAIN
          : RemovalPolicy.DESTROY,
      lifecycleRules: [
        {
          id: 'delete-incomplete-multipart-uploads',
          abortIncompleteMultipartUploadAfter: Duration.days(7),
        },
        {
          id: 'transition-to-ia',
          transitions: [
            {
              storageClass: s3.StorageClass.INFREQUENT_ACCESS,
              transitionAfter: Duration.days(30),
            },
            {
              storageClass: s3.StorageClass.GLACIER,
              transitionAfter: Duration.days(90),
            },
          ],
        },
      ],
      cors: [
        {
          allowedMethods: [
            s3.HttpMethods.GET,
            s3.HttpMethods.POST,
            s3.HttpMethods.PUT,
            s3.HttpMethods.DELETE,
            s3.HttpMethods.HEAD,
          ],
          allowedOrigins: [
            'http://localhost:4200',
            'https://localhost:4200',
            'http://localhost:3000',
            'https://localhost:3000',
            '*', // Allow all origins for development
          ],
          allowedHeaders: [
            'Content-Type',
            'Content-Length',
            'Authorization',
            'x-amz-date',
            'x-amz-content-sha256',
            'x-amz-meta-*',
          ],
          exposedHeaders: ['ETag', 'x-amz-version-id'],
          maxAge: 3000,
        },
      ],
    });

    // Add tags to the S3 bucket
    this.petMediaBucket.node.addMetadata(
      'Description',
      'S3 bucket for storing pet images and videos'
    );
  }
}
