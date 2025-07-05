import { Construct } from 'constructs';
import { BaseStack } from './base-stack';
import { PawfectMatchStackProps } from './utils';
import * as dynamodb from 'aws-cdk-lib/aws-dynamodb';

export interface ShelterHubStackProps extends PawfectMatchStackProps {}

export class ShelterHubStack extends BaseStack {
  public readonly shelterAdminsTable: dynamodb.Table;
  public readonly sheltersTable: dynamodb.Table;

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
  }
}
