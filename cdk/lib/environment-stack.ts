import * as cdk from 'aws-cdk-lib';
import { Construct } from 'constructs';
import { SharedStack } from './shared-stack';

export interface EnvironmentStackProps extends cdk.StackProps {
  stage: string; // 'development' or 'production'
  sharedStack: SharedStack; // Reference to the shared stack
  // Add other properties as needed
}

export class EnvironmentStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props: EnvironmentStackProps) {
    super(scope, id, props);

    const { stage, sharedStack } = props;
  }
}
