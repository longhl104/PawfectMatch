import { Environment } from './environment.interface';

export const environment: Environment = {
  production: false,
  googleMapsApiKey: 'YOUR_GOOGLE_MAPS_API_KEY_HERE',
  apiUrl: 'http://localhost:5200/api',
  appName: 'PawfectMatch',
  version: '1.0.0',
  shelterHubUrl: 'http://localhost:5202',
  matcherUrl: 'http://localhost:5201',
};
