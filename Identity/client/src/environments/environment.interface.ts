export interface Environment {
  environmentName: 'production' | 'development' | 'local';
  googleMapsApiKey: string;
  apiUrl: string;
  appName: string;
  version: string;
  shelterHubUrl: string;
  matcherUrl: string;
}
