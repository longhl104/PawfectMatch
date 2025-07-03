import { Injectable, inject } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Observable, map, take } from 'rxjs';
import { AuthService, AuthStatusResponse } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  private authService = inject(AuthService);
  private router = inject(Router);

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
          return false;
        }
      })
    );
  }
}
