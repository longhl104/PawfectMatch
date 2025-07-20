import * as cdk from 'aws-cdk-lib';
import { SharedStack } from '../shared-stack';
import { EnvironmentStack } from '../environment-stack';
import { DomainConfig } from './domain-config';

export interface PawfectMatchBaseStackProps extends cdk.StackProps {
  stage: StageType; // Stage of the application
  sharedStack: SharedStack;
  environmentStack: EnvironmentStack;
  serviceName: string; // Name of the service (e.g., 'Identity', 'Matcher')
  domainConfig?: DomainConfig; // Domain configuration for the service
}

export type StageType = 'development' | 'production';
