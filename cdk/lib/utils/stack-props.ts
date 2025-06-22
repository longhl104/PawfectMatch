import * as cdk from 'aws-cdk-lib';
import { SharedStack } from '../shared-stack';
import { EnvironmentStack } from '../environment-stack';

export interface PawfectMatchStackProps extends cdk.StackProps {
  stage: string;
  sharedStack: SharedStack;
  environmentStack: EnvironmentStack;
}
