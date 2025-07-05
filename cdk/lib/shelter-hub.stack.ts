import { Construct } from 'constructs';
import { BaseStack } from './base-stack';
import { PawfectMatchStackProps } from './utils';

export interface ShelterHubStackProps extends PawfectMatchStackProps {}

export class ShelterHubStack extends BaseStack {
  constructor(scope: Construct, id: string, props: ShelterHubStackProps) {
    super(scope, id, props);

    const { stage } = props;
  }
}
