import * as cdk from 'aws-cdk-lib';
import * as ssm from 'aws-cdk-lib/aws-ssm';
import * as dynamodb from 'aws-cdk-lib/aws-dynamodb';
import { RemovalPolicy } from 'aws-cdk-lib';
import { PawfectMatchBaseStackProps, StageType } from './utils';
import { Construct } from 'constructs';

export interface DynamoDbTableConfig {
  tableName: string;
  partitionKey: {
    name: string;
    type: dynamodb.AttributeType;
  };
  sortKey?: {
    name: string;
    type: dynamodb.AttributeType;
  };
  globalSecondaryIndexes?: {
    indexName: string;
    partitionKey: {
      name: string;
      type: dynamodb.AttributeType;
    };
    sortKey?: {
      name: string;
      type: dynamodb.AttributeType;
    };
    nonKeyAttributes?: string[];
  }[];
  description?: string;
}

export class BaseStack extends cdk.Stack {
  protected readonly stage: StageType;
  protected readonly sharedStack: cdk.Stack;
  protected readonly environmentStack: cdk.Stack;
  protected readonly serviceName: string;

  constructor(scope: Construct, id: string, props: PawfectMatchBaseStackProps) {
    super(scope, id, props);

    this.stage = props.stage;
    this.sharedStack = props.sharedStack;
    this.environmentStack = props.environmentStack;
    this.serviceName = props.serviceName;
  }

  static getCapitalizedStage(stage: StageType): string {
    return stage.charAt(0).toUpperCase() + stage.slice(1);
  }

  /**
   * Creates an SSM parameter with a standardized naming convention
   * @param id The construct ID for the parameter
   * @param stage The deployment stage
   * @param service The service name (e.g., 'Identity', 'Matcher')
   * @param parameterPath The parameter name (e.g., 'UserPoolId', 'ApiKey')
   * @param value The parameter value
   * @param description Optional description for the parameter
   * @returns The created SSM StringParameter
   */
  protected createSsmParameter(
    parameterPath: string,
    value: string,
    description?: string
  ): ssm.StringParameter {
    const id = `${this.stackName}-${parameterPath.replace(/\//g, '-')}`;
    return new ssm.StringParameter(this, id, {
      parameterName: `/PawfectMatch/${BaseStack.getCapitalizedStage(
        this.stage
      )}/${this.serviceName}/${parameterPath}`,
      stringValue: value,
      description:
        description || `${parameterPath} for ${this.stage} environment`,
    });
  }

  /**
   * Creates a DynamoDB table with standardized configuration based on stage
   * @param id The construct ID for the table
   * @param stage The deployment stage
   * @param config The table configuration
   * @returns The created DynamoDB Table
   */
  protected createDynamoDbTable(config: DynamoDbTableConfig): dynamodb.Table {
    const tableName = `${this.stackName}-${config.tableName}`;

    const tableProps: dynamodb.TableProps = {
      tableName: tableName,
      partitionKey: config.partitionKey,
      ...(config.sortKey && { sortKey: config.sortKey }),
      billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
      pointInTimeRecoverySpecification: {
        pointInTimeRecoveryEnabled: this.stage === 'production',
      },
      deletionProtection: this.stage === 'production',
      removalPolicy:
        this.stage === 'production'
          ? RemovalPolicy.RETAIN
          : RemovalPolicy.DESTROY,
    };

    // Create the table
    const table = new dynamodb.Table(this, tableName, tableProps);

    // Add Global Secondary Indexes if provided
    if (config.globalSecondaryIndexes) {
      config.globalSecondaryIndexes.forEach((gsi) => {
        const gsiProps: dynamodb.GlobalSecondaryIndexProps = {
          indexName: gsi.indexName,
          partitionKey: gsi.partitionKey,
          ...(gsi.sortKey && { sortKey: gsi.sortKey }),
          projectionType: dynamodb.ProjectionType.INCLUDE,
          nonKeyAttributes: gsi.nonKeyAttributes,
        };

        table.addGlobalSecondaryIndex(gsiProps);
      });
    }

    return table;
  }
}
