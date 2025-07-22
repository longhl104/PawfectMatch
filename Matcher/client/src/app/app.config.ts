import {
  ApplicationConfig,
  ErrorHandler,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
  provideAppInitializer,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import {
  provideClientHydration,
  withEventReplay,
} from '@angular/platform-browser';
import {
  HttpInterceptorFn,
  provideHttpClient,
  withFetch,
  withInterceptors,
  withInterceptorsFromDi,
} from '@angular/common/http';
import { GlobalErrorHandler } from '@longhl104/pawfect-match-ng';
import { authInitializer } from './initializers/auth.initializer';

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
    provideHttpClient(
      withFetch(),
      withInterceptorsFromDi(),
      withInterceptors([credentialsInterceptor]),
    ),
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    provideAppInitializer(authInitializer()),
  ],
};
