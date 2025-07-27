import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface UserProfile {
  userId: string;
  email: string;
  fullName: string;
  userType: 'adopter' | 'shelter';
  verified: boolean;
}

export interface AuthStatusResponse {
  isAuthenticated: boolean;
  message: string;
  redirectUrl?: string;
  requiresRefresh?: boolean;
  user?: UserProfile;
  tokenExpiresAt?: string;
  throwError?: boolean; // Added for error handling
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiUrl}/api/authcheck`;
  private readonly identityUrl = environment.identityUrl;

  private authStatusSubject = new BehaviorSubject<AuthStatusResponse>({
    isAuthenticated: false,
    message: 'Not checked yet',
  });

  public authStatus$ = this.authStatusSubject.asObservable();

  private currentUserSubject = new BehaviorSubject<UserProfile | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor() {
    // Authentication check is now handled by APP_INITIALIZER
    // in app.config.ts for better control over app startup
  }

  /**
   * Check authentication status with the backend
   */
  checkAuthStatus(): Observable<AuthStatusResponse> {
    return this.http
      .get<AuthStatusResponse>(`${this.apiUrl}/status`, {
        withCredentials: true,
      })
      .pipe(
        tap((response) => {
          this.authStatusSubject.next(response);
          this.currentUserSubject.next(response.user || null);
        }),
        catchError((error) => {
          console.error('Auth status check failed:', error);

          const message =
            error.status === 403
              ? 'Access denied. You do not have permission to access this service. Please log in with an adopter account.'
              : 'Authentication check failed';

          // For other errors (401, network issues, etc.), handle gracefully
          const errorResponse: AuthStatusResponse = {
            isAuthenticated: false,
            message,
            redirectUrl: `${this.identityUrl}/auth/login`,
            throwError: error.status === 403,
          };

          this.authStatusSubject.next(errorResponse);
          this.currentUserSubject.next(null);
          return of(errorResponse);
        }),
      );
  }

  /**
   * Get current authentication status
   */
  getCurrentAuthStatus(): AuthStatusResponse {
    return this.authStatusSubject.value;
  }

  /**
   * Get current user
   */
  getCurrentUser(): UserProfile | null {
    return this.currentUserSubject.value;
  }

  /**
   * Check if user is authenticated
   */
  isAuthenticated(): boolean {
    return this.authStatusSubject.value.isAuthenticated;
  }

  /**
   * Logout user
   */
  logout(): Observable<AuthStatusResponse> {
    return this.http
      .post<AuthStatusResponse>(
        `${this.apiUrl}/logout`,
        {},
        {
          withCredentials: true,
        },
      )
      .pipe(
        tap((response) => {
          this.authStatusSubject.next(response);
          this.currentUserSubject.next(null);
          // Redirect to login page
          if (response.redirectUrl) {
            window.location.href = response.redirectUrl;
          }
        }),
        catchError((error) => {
          console.error('Logout failed:', error);
          // Clear local state anyway
          const errorResponse: AuthStatusResponse = {
            isAuthenticated: false,
            message: 'Logout failed',
            redirectUrl: `${this.identityUrl}/auth/login`,
          };
          this.authStatusSubject.next(errorResponse);
          this.currentUserSubject.next(null);
          return of(errorResponse);
        }),
      );
  }

  /**
   * Redirect to login page
   */
  redirectToLogin(): void {
    const loginUrl = `${this.identityUrl}/auth/login`;
    window.location.href = loginUrl;
  }

  redirectToSignUp() {
    window.location.href = `${this.identityUrl}/auth/choice`;
  }

  /**
   * Get identity application URL
   */
  getIdentityUrl(): string {
    return this.identityUrl;
  }
}
