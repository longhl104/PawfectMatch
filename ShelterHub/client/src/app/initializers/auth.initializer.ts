import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AuthService } from 'shared/services/auth.service';
import { firstValueFrom } from 'rxjs';
import { ShelterService } from 'shared/services/shelter.service';

/**
 * Auth initializer function for APP_INITIALIZER
 * Ensures authentication status is checked before the app starts
 */
export function authInitializer(): () => Promise<void> {
  return async () => {
    const authService = inject(AuthService);
    const platformId = inject(PLATFORM_ID);
    const shelterService = inject(ShelterService);

    // Only check auth status in browser environment
    if (!isPlatformBrowser(platformId)) {
      console.log('Skipping auth initialization on server');
      return Promise.resolve();
    }

    console.log('Initializing authentication...');
    try {
      // await firstValueFrom(authService.checkAuthStatus());
      await Promise.all([
        firstValueFrom(authService.checkAuthStatus()),
        shelterService.getShelterInfo(),
      ]);

      console.log('Authentication initialization completed');
    } catch (error) {
      console.error('Authentication initialization failed:', error);
    }
  };
}
