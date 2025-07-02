import { Environment } from './environment.interface';

export const environment: Environment = {
  production: false,
  googleMapsApiKey: 'YOUR_GOOGLE_MAPS_API_KEY_HERE',
  apiUrl: 'http://localhost:5200/api',
  identityServiceUrl: 'http://localhost:5000',
  appName: 'PawfectMatch',
  version: '1.0.0',
};
