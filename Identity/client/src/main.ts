import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { setGlobalInjector } from '@longhl104/pawfect-match-ng';

bootstrapApplication(App, appConfig)
  .then((appRef) => {
    // Set the global injector for error handling utilities
    setGlobalInjector(appRef.injector);
  })
  .catch((err) => console.error(err));
