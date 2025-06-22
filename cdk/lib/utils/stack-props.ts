import * as cdk from 'aws-cdk-lib';
import { SharedStack } from '../shared-stack';

export interface PawfectMatchStackProps extends cdk.StackProps {
  stage: string;
  sharedStack: SharedStack;
}
