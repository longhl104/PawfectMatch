import * as cdk from 'aws-cdk-lib';
import { SharedStack } from '../shared-stack';
import { EnvironmentStack } from '../environment-stack';

export interface PawfectMatchStackProps extends cdk.StackProps {
  stage: StageType; // Stage of the application
  sharedStack: SharedStack;
  environmentStack: EnvironmentStack;
}

export type StageType = 'development' | 'production';
