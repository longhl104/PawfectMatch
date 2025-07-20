import { StageType } from './stack-props';

export interface DomainConfig {
  domainName: string;
  certificateArn?: string;
  createHostedZone: boolean;
}

export interface ServiceDomainConfig {
  identity: DomainConfig;
  matcher: DomainConfig;
  shelterHub: DomainConfig;
}

export class DomainConfigManager {
  private static readonly ROOT_DOMAIN = 'pawfectmatchnow.com';

  /**
   * Get domain configuration for a specific stage
   */
  static getDomainConfig(stage: StageType): ServiceDomainConfig {
    const baseDomain = stage === 'production'
      ? this.ROOT_DOMAIN
      : `${stage}.${this.ROOT_DOMAIN}`;

    return {
      identity: {
        domainName: stage === 'production'
          ? `id.${this.ROOT_DOMAIN}`
          : `id.${baseDomain}`,
        createHostedZone: stage === 'production',
      },
      matcher: {
        domainName: stage === 'production'
          ? `adopter.${this.ROOT_DOMAIN}`
          : `adopter.${baseDomain}`,
        createHostedZone: false, // Use the same hosted zone as root
      },
      shelterHub: {
        domainName: stage === 'production'
          ? `shelter.${this.ROOT_DOMAIN}`
          : `shelter.${baseDomain}`,
        createHostedZone: false, // Use the same hosted zone as root
      },
    };
  }

  /**
   * Get the root domain for the stage
   */
  static getRootDomain(stage: StageType): string {
    return stage === 'production'
      ? this.ROOT_DOMAIN
      : `${stage}.${this.ROOT_DOMAIN}`;
  }

  /**
   * Get hosted zone name for the stage
   */
  static getHostedZoneName(stage: StageType): string {
    return stage === 'production'
      ? this.ROOT_DOMAIN
      : `${stage}.${this.ROOT_DOMAIN}`;
  }
}
