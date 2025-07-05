import * as cdk from 'aws-cdk-lib';
import { SharedStack } from '../shared-stack';
import { EnvironmentStack } from '../environment-stack';

export interface PawfectMatchBaseStackProps extends cdk.StackProps {
  stage: StageType; // Stage of the application
  sharedStack: SharedStack;
  environmentStack: EnvironmentStack;
  serviceName: string; // Name of the service (e.g., 'Identity', 'Matcher')
}

export type StageType = 'development' | 'production';
