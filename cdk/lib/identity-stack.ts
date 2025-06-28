import * as cognito from 'aws-cdk-lib/aws-cognito';
import * as dynamodb from 'aws-cdk-lib/aws-dynamodb';
import * as lambda from 'aws-cdk-lib/aws-lambda';
import * as apigateway from 'aws-cdk-lib/aws-apigateway';
import * as iam from 'aws-cdk-lib/aws-iam';
import { Construct } from 'constructs';
import { LambdaUtils, PawfectMatchStackProps } from './utils';
import { BaseStack } from './base-stack';
import { Duration, Fn, RemovalPolicy } from 'aws-cdk-lib';

export interface IdentityStackProps extends PawfectMatchStackProps {}

export class IdentityStack extends BaseStack {
  public readonly userPool: cognito.UserPool;
  public readonly userPoolClient: cognito.UserPoolClient;
  public readonly adoptersTable: dynamodb.Table;
  public readonly refreshTokensTable: dynamodb.Table;
  public readonly registerAdopterFunction: lambda.Function;
  public readonly loginFunction: lambda.Function;
  public readonly refreshTokenFunction: lambda.Function;

  constructor(scope: Construct, id: string, props: IdentityStackProps) {
    super(scope, id, props);

    const { stage, sharedStack, environmentStack } = props;

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

      // Custom attributes
      customAttributes: {
        user_type: new cognito.StringAttribute({
          mutable: false,
          minLen: 1,
          maxLen: 50,
        }),
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
        readAttributes: new cognito.ClientAttributes()
          .withStandardAttributes({
            email: true,
            phoneNumber: true,
            address: true,
          })
          .withCustomAttributes('user_type'),

        writeAttributes: new cognito.ClientAttributes()
          .withStandardAttributes({
            email: true,
            phoneNumber: true,
            address: true,
          })
          .withCustomAttributes('user_type'),
      }
    );

    // Create DynamoDB table for Adopters with stream enabled
    this.adoptersTable = new dynamodb.Table(this, 'AdoptersTable', {
      tableName: `pawfect-match-adopters-${stage}`,
      partitionKey: {
        name: 'UserId',
        type: dynamodb.AttributeType.STRING,
      },
      billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
      stream: dynamodb.StreamViewType.NEW_AND_OLD_IMAGES, // Enable DynamoDB streams
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

    // Create DynamoDB table for Refresh Tokens
    this.refreshTokensTable = new dynamodb.Table(this, 'RefreshTokensTable', {
      tableName: `pawfect-match-refresh-tokens-${stage}`,
      partitionKey: {
        name: 'UserId',
        type: dynamodb.AttributeType.STRING,
      },
      sortKey: {
        name: 'RefreshToken',
        type: dynamodb.AttributeType.STRING,
      },
      billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
      timeToLiveAttribute: 'ExpiresAt', // Auto-delete expired tokens
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

    // Create Lambda function for adopter registration
    this.registerAdopterFunction = LambdaUtils.createFunction(
      this,
      'RegisterAdopterFunction',
      'Identity',
      stage,
      {
        functionName: 'RegisterAdopter',
        environment: {
          USER_POOL_ID: this.userPool.userPoolId,
          USER_POOL_CLIENT_ID: this.userPoolClient.userPoolClientId,
          ADOPTERS_TABLE_NAME: this.adoptersTable.tableName,
          STAGE: stage,
        },
        description: 'Lambda function to register new adopters',
      }
    );

    // Create Lambda function for user login
    this.loginFunction = LambdaUtils.createFunction(
      this,
      'LoginFunction',
      'Identity',
      stage,
      {
        functionName: 'UserLogin',
        environment: {
          USER_POOL_ID: this.userPool.userPoolId,
          USER_POOL_CLIENT_ID: this.userPoolClient.userPoolClientId,
          STAGE: stage,
        },
        description: 'Lambda function to authenticate users and issue tokens',
      }
    );

    // Create Lambda function to refresh tokens
    this.refreshTokenFunction = LambdaUtils.createFunction(
      this,
      'RefreshTokenFunction',
      'Identity',
      stage,
      {
        functionName: 'RefreshToken',
        environment: {
          USER_POOL_ID: this.userPool.userPoolId,
          USER_POOL_CLIENT_ID: this.userPoolClient.userPoolClientId,
          STAGE: stage,
        },
        description: 'Lambda function to refresh user tokens',
      }
    );

    // Grant Lambda permissions to access Cognito
    this.registerAdopterFunction.addToRolePolicy(
      new iam.PolicyStatement({
        effect: iam.Effect.ALLOW,
        actions: [
          'cognito-idp:AdminCreateUser',
          'cognito-idp:AdminSetUserPassword',
          'cognito-idp:AdminUpdateUserAttributes',
          'cognito-idp:AdminDeleteUser',
          'cognito-idp:AdminGetUser',
          'cognito-idp:ListUsers',
          'cognito-idp:AdminConfirmSignUp',
        ],
        resources: [this.userPool.userPoolArn],
      })
    );

    // Grant Lambda permissions to access DynamoDB
    this.adoptersTable.grantReadWriteData(this.registerAdopterFunction);

    // Create Lambda function for user login
    this.loginFunction = LambdaUtils.createFunction(
      this,
      'LoginFunction',
      'Identity',
      stage,
      {
        functionName: 'Login',
        environment: {
          USER_POOL_ID: this.userPool.userPoolId,
          USER_POOL_CLIENT_ID: this.userPoolClient.userPoolClientId,
          ADOPTERS_TABLE_NAME: this.adoptersTable.tableName,
          REFRESH_TOKENS_TABLE_NAME: this.refreshTokensTable.tableName,
          STAGE: stage,
          JWT_SECRET: stage === 'production' ? '${aws:ssm:/pawfect-match/jwt-secret}' : 'dev-jwt-secret-key',
          JWT_EXPIRES_IN: '3600', // 1 hour
          REFRESH_TOKEN_EXPIRES_IN: '2592000', // 30 days
        },
        description: 'Lambda function to handle user login with JWT',
      }
    );

    // Create Lambda function for token refresh
    this.refreshTokenFunction = LambdaUtils.createFunction(
      this,
      'RefreshTokenFunction',
      'Identity',
      stage,
      {
        functionName: 'RefreshToken',
        environment: {
          USER_POOL_ID: this.userPool.userPoolId,
          USER_POOL_CLIENT_ID: this.userPoolClient.userPoolClientId,
          ADOPTERS_TABLE_NAME: this.adoptersTable.tableName,
          REFRESH_TOKENS_TABLE_NAME: this.refreshTokensTable.tableName,
          STAGE: stage,
          JWT_SECRET: stage === 'production' ? '${aws:ssm:/pawfect-match/jwt-secret}' : 'dev-jwt-secret-key',
          JWT_EXPIRES_IN: '3600', // 1 hour
          REFRESH_TOKEN_EXPIRES_IN: '2592000', // 30 days
        },
        description: 'Lambda function to refresh JWT tokens',
      }
    );

    // Grant Lambda permissions to access Cognito for login function
    this.loginFunction.addToRolePolicy(
      new iam.PolicyStatement({
        effect: iam.Effect.ALLOW,
        actions: [
          'cognito-idp:AdminInitiateAuth',
          'cognito-idp:AdminGetUser',
          'cognito-idp:AdminRespondToAuthChallenge',
          'cognito-idp:AdminSetUserPassword',
        ],
        resources: [this.userPool.userPoolArn],
      })
    );

    // Grant Lambda permissions to access Cognito for refresh token function
    this.refreshTokenFunction.addToRolePolicy(
      new iam.PolicyStatement({
        effect: iam.Effect.ALLOW,
        actions: [
          'cognito-idp:AdminInitiateAuth',
          'cognito-idp:AdminGetUser',
        ],
        resources: [this.userPool.userPoolArn],
      })
    );

    // Grant DynamoDB access to login and refresh token functions
    this.adoptersTable.grantReadWriteData(this.loginFunction);
    this.adoptersTable.grantReadWriteData(this.refreshTokenFunction);
    this.refreshTokensTable.grantReadWriteData(this.loginFunction);
    this.refreshTokensTable.grantReadWriteData(this.refreshTokenFunction);

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

    this.exportValue(this.refreshTokensTable.tableName, {
      name: `${stage}RefreshTokensTableName`,
    });

    this.exportValue(this.refreshTokensTable.tableArn, {
      name: `${stage}RefreshTokensTableArn`,
    });

    // Use API Gateway from environment stack
    const api = apigateway.RestApi.fromRestApiAttributes(
      this,
      'PawfectMatchApi',
      {
        restApiId: Fn.importValue(`${stage}ApiGatewayId`),
        rootResourceId: Fn.importValue(`${stage}ApiGatewayRootResourceId`),
      }
    );

    const identityResource = api.root.addResource('identity');
    const adoptersResource = identityResource.addResource('adopters');
    const usersResource = identityResource.addResource('users');

    // Create Lambda integrations
    const registerAdopterIntegration = new apigateway.LambdaIntegration(
      this.registerAdopterFunction,
      {
        requestTemplates: { 'application/json': '{ "statusCode": "200" }' },
        proxy: true,
      }
    );

    const loginIntegration = new apigateway.LambdaIntegration(
      this.loginFunction,
      {
        requestTemplates: { 'application/json': '{ "statusCode": "200" }' },
        proxy: true,
      }
    );

    const refreshTokenIntegration = new apigateway.LambdaIntegration(
      this.refreshTokenFunction,
      {
        requestTemplates: { 'application/json': '{ "statusCode": "200" }' },
        proxy: true,
      }
    );

    // Add POST /identity/adopters/register endpoint
    adoptersResource
      .addResource('register')
      .addMethod('POST', registerAdopterIntegration, {
        methodResponses: [
          {
            statusCode: '200',
            responseModels: {
              'application/json': apigateway.Model.EMPTY_MODEL,
            },
          },
          {
            statusCode: '400',
            responseModels: {
              'application/json': apigateway.Model.ERROR_MODEL,
            },
          },
          {
            statusCode: '500',
            responseModels: {
              'application/json': apigateway.Model.ERROR_MODEL,
            },
          },
        ],
      });

    // Add POST /identity/users/login endpoint
    usersResource
      .addResource('login')
      .addMethod('POST', loginIntegration, {
        methodResponses: [
          {
            statusCode: '200',
            responseModels: {
              'application/json': apigateway.Model.EMPTY_MODEL,
            },
          },
          {
            statusCode: '400',
            responseModels: {
              'application/json': apigateway.Model.ERROR_MODEL,
            },
          },
          {
            statusCode: '500',
            responseModels: {
              'application/json': apigateway.Model.ERROR_MODEL,
            },
          },
        ],
      });

    // Add POST /identity/users/refresh-token endpoint
    usersResource
      .addResource('refresh-token')
      .addMethod('POST', refreshTokenIntegration, {
        methodResponses: [
          {
            statusCode: '200',
            responseModels: {
              'application/json': apigateway.Model.EMPTY_MODEL,
            },
          },
          {
            statusCode: '400',
            responseModels: {
              'application/json': apigateway.Model.ERROR_MODEL,
            },
          },
          {
            statusCode: '500',
            responseModels: {
              'application/json': apigateway.Model.ERROR_MODEL,
            },
          },
        ],
      });
  }
}
