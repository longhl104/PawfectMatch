import * as cognito from 'aws-cdk-lib/aws-cognito';
import { Construct } from 'constructs';
import { PawfectMatchBaseStackProps } from './utils';
import { BaseStack } from './base-stack';
import { Duration, RemovalPolicy } from 'aws-cdk-lib';

export interface IdentityStackProps extends PawfectMatchBaseStackProps {}

export class IdentityStack extends BaseStack {
  public readonly userPool: cognito.UserPool;
  public readonly userPoolClient: cognito.UserPoolClient;

  constructor(scope: Construct, id: string, props: IdentityStackProps) {
    super(scope, id, props);

    const { stage } = props;

    // Create Cognito User Pool
    this.userPool = new cognito.UserPool(this, `${this.stackName}-user-pool`, {
      userPoolName: `${this.stackName}-user-pool`,

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
      `${this.stackName}-user-pool-client`,
      {
        userPool: this.userPool,
        userPoolClientName: `${this.stackName}-user-pool-client`,

        // Auth flows
        authFlows: {
          userSrp: true,
          userPassword: false,
          custom: false,
          adminUserPassword: true,
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

    // Store User Pool ID in Parameter Store
    this.createSsmParameter(
      'UserPoolId',
      this.userPool.userPoolId,
      `User Pool ID for ${stage} environment`
    );

    this.createSsmParameter(
      'UserPoolClientId',
      this.userPoolClient.userPoolClientId,
      `User Pool Client ID for ${stage} environment`
    );
  }
}
