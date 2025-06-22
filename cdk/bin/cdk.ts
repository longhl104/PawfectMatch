#!/usr/bin/env node
import * as cdk from 'aws-cdk-lib';
import { SharedStack, IdentityStack, EnvironmentStack } from '../lib';

const app = new cdk.App();

// Environment configuration
const environments = {
  development: {
    account: process.env.CDK_DEFAULT_ACCOUNT, // Account A
    region: process.env.CDK_DEFAULT_REGION,
  },
  production: {
    account: process.env.CDK_PRODUCTION_ACCOUNT, // Account B
    region: process.env.CDK_DEFAULT_REGION,
  },
};

const stage = process.env.CDK_STAGE;
if (!stage) {
  throw new Error(
    'CDK_STAGE environment variable is not set. Please set it to "development" or "production".'
  );
}

// Validate environment
if (!environments[stage as keyof typeof environments]) {
  throw new Error(
    `Invalid stage: ${stage}. Must be one of: ${Object.keys(environments).join(
      ', '
    )}`
  );
}

const env = environments[stage as keyof typeof environments];

// Create Shared Stack (one per account)
const sharedStack = new SharedStack(app, 'PawfectMatch-Shared', {
  env,
  stackName: 'pawfectmatch-shared',
});

// Create Environment Stack (one per stage)
const environmentStack = new EnvironmentStack(
  app,
  `PawfectMatch-Environment-${stage}`,
  {
    env,
    stackName: `pawfectmatch-environment-${stage}`,
    stage,
    sharedStack, // Pass the shared stack reference
  }
);

// Create Identity Stack
const identityStack = new IdentityStack(app, `PawfectMatch-Identity-${stage}`, {
  env,
  stage,
  sharedStack,
  environmentStack,
  stackName: `pawfectmatch-identity-${stage}`,
});

// Add dependency
environmentStack.addDependency(sharedStack);

identityStack.addDependency(sharedStack);
identityStack.addDependency(environmentStack);

// Add tags to all stacks
cdk.Tags.of(app).add('Project', 'PawfectMatch');
cdk.Tags.of(app).add('Stage', stage);
