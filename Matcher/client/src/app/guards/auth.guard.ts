import { Injectable, inject } from '@angular/core';
import { CanActivate } from '@angular/router';
import { Observable, map, take } from 'rxjs';
import { AuthService, AuthStatusResponse } from '../services/auth.service';
import { ToastService } from '@longhl104/pawfect-match-ng';

@Injectable({
  providedIn: 'root',
})
export class AuthGuard implements CanActivate {
  private authService = inject(AuthService);
  private toastService = inject(ToastService);

  canActivate(): Observable<boolean> {
    return this.authService.authStatus$.pipe(
      take(1),
      map((authStatus: AuthStatusResponse) => {
        if (authStatus.isAuthenticated) {
          return true;
        } else {
          // Redirect to login page
          if (authStatus.redirectUrl) {
            window.location.href = authStatus.redirectUrl;
          } else {
            this.authService.redirectToLogin();
          }

          this.toastService.error(
            "You don't have permission to access this page. Please log in.",
          );

          return false;
        }
      }),
    );
  }
}
