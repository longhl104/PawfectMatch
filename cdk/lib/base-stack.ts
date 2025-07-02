import * as cdk from 'aws-cdk-lib';
import { StageType } from './utils';

export class BaseStack extends cdk.Stack {
  static getCapitalizedStage(stage: StageType): string {
    return stage.charAt(0).toUpperCase() + stage.slice(1);
  }
}
