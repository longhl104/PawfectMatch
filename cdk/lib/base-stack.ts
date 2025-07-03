import * as cdk from 'aws-cdk-lib';
import * as ssm from 'aws-cdk-lib/aws-ssm';
import { StageType } from './utils';

export class BaseStack extends cdk.Stack {
  static getCapitalizedStage(stage: StageType): string {
    return stage.charAt(0).toUpperCase() + stage.slice(1);
  }

  /**
   * Creates an SSM parameter with a standardized naming convention
   * @param id The construct ID for the parameter
   * @param stage The deployment stage
   * @param service The service name (e.g., 'Identity', 'Matcher')
   * @param category The category (e.g., 'AWS', 'Config')
   * @param parameterName The parameter name (e.g., 'UserPoolId', 'ApiKey')
   * @param value The parameter value
   * @param description Optional description for the parameter
   * @returns The created SSM StringParameter
   */
  protected createSsmParameter(
    id: string,
    stage: StageType,
    service: string,
    parameterName: string,
    value: string,
    description?: string
  ): ssm.StringParameter {
    return new ssm.StringParameter(this, id, {
      parameterName: `/PawfectMatch/${BaseStack.getCapitalizedStage(
        stage
      )}/${service}/${parameterName}`,
      stringValue: value,
      description: description || `${parameterName} for ${stage} environment`,
    });
  }
}
