import * as cognito from 'aws-cdk-lib/aws-cognito';
import * as apigateway from 'aws-cdk-lib/aws-apigateway';
import * as dynamodb from 'aws-cdk-lib/aws-dynamodb';
import { Construct } from 'constructs';
import { PawfectMatchStackProps } from './utils';
import { BaseStack } from './base-stack';
import { Duration, RemovalPolicy } from 'aws-cdk-lib';

export interface IdentityStackProps extends PawfectMatchStackProps {}

export class IdentityStack extends BaseStack {
  public readonly userPool: cognito.UserPool;
  public readonly userPoolClient: cognito.UserPoolClient;
  public readonly adoptersTable: dynamodb.Table;
  public readonly api: apigateway.RestApi;

  constructor(scope: Construct, id: string, props: IdentityStackProps) {
    super(scope, id, props);

    const { stage, sharedStack } = props;

    // Create Cognito User Pool
    this.userPool = new cognito.UserPool(this, 'PawfectMatchUserPool', {
      userPoolName: `pawfect-match-user-pool-${stage}`,

      // Sign-in configuration
      signInAliases: {
        email: true,
        username: false,
        phone: false,
      },

      // Auto-verification
      autoVerify: {
        email: true,
      },

      // Required attributes - only email is required
      standardAttributes: {
        email: {
          required: true,
          mutable: true,
        },
        phoneNumber: {
          required: false,
          mutable: true,
        },
        address: {
          required: false,
          mutable: true,
        },
      },

      // Password policy
      passwordPolicy: {
        minLength: 8,
        requireLowercase: true,
        requireUppercase: true,
        requireDigits: true,
        requireSymbols: true,
      },

      // Account recovery
      accountRecovery: cognito.AccountRecovery.EMAIL_AND_PHONE_WITHOUT_MFA,

      // Email settings
      email: cognito.UserPoolEmail.withCognito(),

      // Device tracking
      deviceTracking: {
        challengeRequiredOnNewDevice: true,
        deviceOnlyRememberedOnUserPrompt: false,
      },

      // Lambda triggers (can be added later)
      lambdaTriggers: {
        // preSignUp: preSignUpLambda,
        // postConfirmation: postConfirmationLambda,
      },

      // Deletion protection
      deletionProtection: stage === 'production',

      // Remove default removal policy for production
      removalPolicy: stage === 'production' ? undefined : RemovalPolicy.DESTROY,
    });

    // Create User Pool Client
    this.userPoolClient = new cognito.UserPoolClient(
      this,
      'PawfectMatchUserPoolClient',
      {
        userPool: this.userPool,
        userPoolClientName: `pawfect-match-client-${stage}`,

        // Auth flows
        authFlows: {
          userSrp: true,
          userPassword: false,
          custom: false,
          adminUserPassword: false,
        },

        // OAuth settings
        oAuth: {
          flows: {
            authorizationCodeGrant: true,
            implicitCodeGrant: false,
          },
          scopes: [
            cognito.OAuthScope.EMAIL,
            cognito.OAuthScope.OPENID,
            cognito.OAuthScope.PROFILE,
          ],
          callbackUrls: [
            `https://${
              stage === 'production' ? 'www' : stage
            }.pawfectmatch.com/auth/callback`,
            'http://localhost:4200/auth/callback', // For local development
          ],
          logoutUrls: [
            `https://${
              stage === 'production' ? 'www' : stage
            }.pawfectmatch.com/auth/logout`,
            'http://localhost:4200/auth/logout', // For local development
          ],
        },

        // Token validity
        accessTokenValidity: Duration.hours(1),
        idTokenValidity: Duration.hours(1),
        refreshTokenValidity: Duration.days(30),

        // Prevent user existence errors
        preventUserExistenceErrors: true,

        // Enable token revocation
        enableTokenRevocation: true,

        // Supported identity providers
        supportedIdentityProviders: [
          cognito.UserPoolClientIdentityProvider.COGNITO,
        ],

        // Read and write attributes
        readAttributes: new cognito.ClientAttributes().withStandardAttributes({
          email: true,
          phoneNumber: true,
          address: true,
        }),

        writeAttributes: new cognito.ClientAttributes().withStandardAttributes({
          email: true,
          phoneNumber: true,
          address: true,
        }),
      }
    );

    // Create DynamoDB table for Adopters
    this.adoptersTable = new dynamodb.Table(this, 'AdoptersTable', {
      tableName: `pawfect-match-adopters-${stage}`,
      partitionKey: {
        name: 'userId',
        type: dynamodb.AttributeType.STRING,
      },
      billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
      pointInTimeRecoverySpecification:
        stage === 'production'
          ? {
              pointInTimeRecoveryEnabled: true,
            }
          : undefined,
      deletionProtection: stage === 'production',
      removalPolicy:
        stage === 'production' ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY,
    });

    // Create User Pool Domain (optional, for hosted UI)
    const userPoolDomain = new cognito.UserPoolDomain(
      this,
      'PawfectMatchUserPoolDomain',
      {
        userPool: this.userPool,
        cognitoDomain: {
          domainPrefix: `pawfect-match-auth-${stage}`,
        },
      }
    );

    // Export important values
    this.exportValue(this.userPool.userPoolId, {
      name: `${stage}UserPoolId`,
    });

    this.exportValue(this.userPoolClient.userPoolClientId, {
      name: `${stage}UserPoolClientId`,
    });

    this.exportValue(userPoolDomain.baseUrl(), {
      name: `${stage}UserPoolDomain`,
    });

    this.exportValue(this.userPool.userPoolArn, {
      name: `${stage}UserPoolArn`,
    });

    this.exportValue(this.adoptersTable.tableName, {
      name: `${stage}AdoptersTableName`,
    });

    this.exportValue(this.adoptersTable.tableArn, {
      name: `${stage}AdoptersTableArn`,
    });
  }
}
