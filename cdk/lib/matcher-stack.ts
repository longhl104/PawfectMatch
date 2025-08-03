import { Construct } from 'constructs';
import { BaseStack } from './base-stack';
import { PawfectMatchBaseStackProps } from './utils';
import * as dynamodb from 'aws-cdk-lib/aws-dynamodb';

export interface MatcherStackProps extends PawfectMatchBaseStackProps {}

export class MatcherStack extends BaseStack {
  public readonly adopterTable: dynamodb.Table;

  constructor(scope: Construct, id: string, props: MatcherStackProps) {
    super(scope, id, props);

    const { stage } = props;

    // Create Adopters DynamoDB Table
    this.adopterTable = this.createDynamoDbTable({
      tableName: 'adopters',
      partitionKey: {
        name: 'UserId',
        type: dynamodb.AttributeType.STRING,
      },
      description: 'Table for storing adopter information',
    });

    // Create ECS service for Matcher API with minimal resources
    // this.createEcsService({
    //   repository: this.environmentStack.matcherRepository,
    //   containerPort: 8080,
    //   cpu: 256, // Reduced from 512 to save ~50% on compute costs
    //   memory: 512, // Reduced from 1024 to save ~50% on memory costs
    //   healthCheckPath: '/health',
    //   subdomain: 'api-matcher',
    //   environment: {
    //     'PawfectMatch__Environment': this.stage,
    //     'PawfectMatch__ServiceName': 'Matcher',
    //   },
    // });
  }
}
