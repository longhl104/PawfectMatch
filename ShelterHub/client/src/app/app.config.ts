import {
  ApplicationConfig,
  ErrorHandler,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
  provideAppInitializer,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { routes } from './app.routes';
import {
  provideClientHydration,
  withEventReplay,
} from '@angular/platform-browser';
import {
  provideHttpClient,
  withFetch,
  withInterceptors,
  withInterceptorsFromDi,
} from '@angular/common/http';
import { GlobalErrorHandler, credentialsInterceptor } from '@longhl104/pawfect-match-ng';
import { authInitializer } from './initializers/auth.initializer';
import { providePrimeNG } from 'primeng/config';
import { DialogService } from 'primeng/dynamicdialog';
import Aura from '@primeuix/themes/aura';
import { API_BASE_URL, PetsApi, SheltersApi } from 'shared/apis/generated-apis';
import { environment } from 'environments/environment';

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
    provideAnimationsAsync(),
    providePrimeNG({
      theme: {
        preset: Aura,
        options: {
          prefix: 'p',
          darkModeSelector: false,
          cssLayer: false,
        },
      },
    }),
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    DialogService,
    provideAppInitializer(authInitializer()),
    {
      provide: API_BASE_URL,
      useValue: environment.apiUrl,
    },
    PetsApi,
    SheltersApi,
  ],
};
