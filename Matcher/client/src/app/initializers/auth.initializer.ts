import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AuthService } from '../services/auth.service';
import { firstValueFrom } from 'rxjs';

/**
 * Auth initializer function for APP_INITIALIZER
 * Ensures authentication status is checked before the app starts
 * Only runs on browser platform (not during SSR)
 */
export function authInitializer(): () => Promise<void> {
  return async () => {
    const authService = inject(AuthService);
    const platformId = inject(PLATFORM_ID);

    // Only check auth status in browser environment
    if (!isPlatformBrowser(platformId)) {
      console.log('Skipping auth initialization on server');
      return Promise.resolve();
    }

    console.log('Initializing authentication...');
    try {
      await firstValueFrom(authService.checkAuthStatus());
      console.log('Authentication initialization completed');
    } catch (error) {
      console.error('Authentication initialization failed:', error);
    }
  };
}
