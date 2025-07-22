import {
  ApplicationConfig,
  ErrorHandler,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import {
  provideClientHydration,
  withEventReplay,
} from '@angular/platform-browser';
import {
  provideHttpClient,
  withFetch,
  withInterceptors,
} from '@angular/common/http';
import { GlobalErrorHandler } from '@longhl104/pawfect-match-ng';
import { HttpInterceptorFn } from '@angular/common/http';

// Credentials interceptor to include cookies in all requests
const credentialsInterceptor: HttpInterceptorFn = (req, next) => {
  const reqWithCredentials = req.clone({
    setHeaders: {
      'Content-Type': 'application/json',
    },
    withCredentials: true,
  });

  return next(reqWithCredentials);
};

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideClientHydration(withEventReplay()),
    provideHttpClient(withFetch(), withInterceptors([credentialsInterceptor])),
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
  ],
};
