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
            'AdoptionFee',
            'PetPostgreSqlId',
          ],
        },
      ],
    });

    // Create S3 Bucket for Pet Media with aggressive cost optimization
    this.petMediaBucket = new s3.Bucket(this, `${this.stackName}-pet-media`, {
      bucketName: `${this.stackName}-pet-media`.toLowerCase(),
      versioned: false, // Disable versioning to save storage costs
      removalPolicy: RemovalPolicy.DESTROY, // Allow deletion for all environments
      lifecycleRules: [
        {
          id: 'delete-incomplete-multipart-uploads',
          abortIncompleteMultipartUploadAfter: Duration.days(1), // Faster cleanup
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

    // Create ECS service for ShelterHub API with minimal resources
    // this.createEcsService({
    //   repository: this.environmentStack.shelterHubRepository,
    //   containerPort: 8080,
    //   cpu: 256, // Reduced from 512 to save ~50% on compute costs
    //   memory: 512, // Reduced from 1024 to save ~50% on memory costs
    //   healthCheckPath: '/health',
    //   subdomain: 'api-shelter',
    //   environment: {
    //     PawfectMatch__Environment: this.stage,
    //     PawfectMatch__ServiceName: 'ShelterHub',
    //   },
    // });

    this.createDynamoDbTable({
      tableName: 'pet-media-files',
      partitionKey: {
        name: 'PetId',
        type: dynamodb.AttributeType.STRING,
      },
      sortKey: {
        name: 'DisplayOrder',
        type: dynamodb.AttributeType.NUMBER,
      },
      description: 'Table for storing pet media files',
      globalSecondaryIndexes: [
        {
          indexName: 'MediaFileIdIndex',
          partitionKey: {
            name: 'MediaFileId',
            type: dynamodb.AttributeType.STRING,
          },
          nonKeyAttributes: ['S3Key'],
        },
      ],
    });
  }
}
