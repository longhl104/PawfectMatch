#!/usr/bin/env node
import 'dotenv/config';
import * as cdk from 'aws-cdk-lib';
import {
  SharedStack,
  IdentityStack,
  EnvironmentStack,
  ShelterHubStack,
  MatcherStack,
} from '../lib';
import { StageType } from '../lib/utils';

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

const stage = process.env.CDK_STAGE as StageType;

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
  `PawfectMatch-${stage}-Environment`,
  {
    env,
    stackName: `pawfectmatch-${stage}-environment`,
    stage,
    sharedStack, // Pass the shared stack reference
  }
);

// Create Identity Stack
const identityStack = new IdentityStack(app, `PawfectMatch-${stage}-Identity`, {
  env,
  stage,
  sharedStack,
  environmentStack,
  stackName: `pawfectmatch-${stage}-identity`,
  serviceName: 'Identity',
});

// Create ShelterHub Stack
const shelterHubStack = new ShelterHubStack(
  app,
  `PawfectMatch-${stage}-ShelterHub`,
  {
    env,
    stage,
    sharedStack,
    environmentStack,
    stackName: `pawfectmatch-${stage}-shelter-hub`,
    serviceName: 'ShelterHub',
  }
);

const matcherStack = new MatcherStack(app, `PawfectMatch-${stage}-Matcher`, {
  env,
  stage,
  sharedStack,
  environmentStack,
  stackName: `pawfectmatch-${stage}-matcher`,
  serviceName: 'Matcher',
});

// Add dependency
environmentStack.addDependency(sharedStack);

identityStack.addDependency(sharedStack);
identityStack.addDependency(environmentStack);

shelterHubStack.addDependency(sharedStack);
shelterHubStack.addDependency(environmentStack);

matcherStack.addDependency(sharedStack);
matcherStack.addDependency(environmentStack);

// Add tags to all stacks
cdk.Tags.of(app).add('Project', 'PawfectMatch');
cdk.Tags.of(app).add('Stage', stage);
