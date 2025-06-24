import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { environment } from 'environments/environment';

export interface AdopterRegistrationRequest {
  fullName: string;
  email: string;
  password: string;
  phoneNumber?: string;
  address: string;
  bio?: string;
}

export interface AdopterProfile {
  id: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  address: string;
  addressDetails: {
    streetNumber: string;
    streetName: string;
    suburb: string;
    city: string;
    state: string;
    postcode: string;
    country: string;
    formattedAddress: string;
    latitude: number;
    longitude: number;
  };
  bio?: string;
  location: {
    latitude: number;
    longitude: number;
  };
  isVerified: boolean;
  dateJoined: Date;
  lastActive: Date;
  adoptionHistory: AdoptionRecord[];
  preferences: AdopterPreferences;
}

export interface AdoptionRecord {
  id: string;
  petId: string;
  petName: string;
  shelterId: string;
  shelterName: string;
  adoptionDate: Date;
  status: 'completed' | 'pending' | 'cancelled';
}

export interface AdopterPreferences {
  petTypes: string[];
  petSizes: string[];
  ageRanges: string[];
  activityLevel: string;
  hasChildren: boolean;
  hasOtherPets: boolean;
  livingArrangement: string;
  maxTravelDistance: number;
  notifications: {
    newMatches: boolean;
    adoptionUpdates: boolean;
    newsletter: boolean;
    events: boolean;
  };
}

export interface AdopterLoginRequest {
  email: string;
  password: string;
}

export interface AdopterLoginResponse {
  adopter: AdopterProfile;
  token: string;
  refreshToken: string;
  expiresIn: number;
}

export interface UpdateAdopterProfileRequest {
  fullName?: string;
  phoneNumber?: string;
  address?: string;
  addressDetails?: AdopterProfile['addressDetails'];
  bio?: string;
  location?: {
    latitude: number;
    longitude: number;
  };
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

@Injectable({
  providedIn: 'root',
})
export class AdoptersService {
  private readonly apiUrl = `${environment.apiUrl}/adopters`;
  private readonly authUrl = `${environment.apiUrl}/auth`;

  private currentAdopterSubject = new BehaviorSubject<AdopterProfile | null>(
    null
  );
  public currentAdopter$ = this.currentAdopterSubject.asObservable();

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor(private http: HttpClient) {
    this.initializeAuth();
  }

  /**
   * Initialize authentication state from stored tokens
   */
  private initializeAuth(): void {
    const token = localStorage.getItem('adopter_token');
    const adopter = localStorage.getItem('adopter_profile');

    if (token && adopter) {
      try {
        const adopterProfile = JSON.parse(adopter);
        this.currentAdopterSubject.next(adopterProfile);
        this.isAuthenticatedSubject.next(true);
      } catch (error) {
        console.error('Error parsing stored adopter profile:', error);
        this.clearAuthData();
      }
    }
  }

  /**
   * Register a new adopter
   */
  register(
    request: AdopterRegistrationRequest
  ): Observable<{ message: string; adopterId: string }> {
    return this.http
      .post<{ message: string; adopterId: string }>(
        `${this.apiUrl}/identity/adopters/register`,
        request
      )
      .pipe(catchError(this.handleError));
  }

  /**
   * Login adopter
   */
  login(request: AdopterLoginRequest): Observable<AdopterLoginResponse> {
    return this.http
      .post<AdopterLoginResponse>(`${this.authUrl}/adopter/login`, request)
      .pipe(
        tap((response) => {
          this.setAuthData(response);
        }),
        catchError(this.handleError)
      );
  }

  /**
   * Logout adopter
   */
  logout(): Observable<void> {
    const refreshToken = localStorage.getItem('adopter_refresh_token');

    return this.http
      .post<void>(`${this.authUrl}/adopter/logout`, {
        refreshToken,
      })
      .pipe(
        tap(() => {
          this.clearAuthData();
        }),
        catchError((error) => {
          // Even if logout fails on server, clear local data
          this.clearAuthData();
          return throwError(() => error);
        })
      );
  }

  /**
   * Refresh authentication token
   */
  refreshToken(): Observable<AdopterLoginResponse> {
    const refreshToken = localStorage.getItem('adopter_refresh_token');

    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }

    return this.http
      .post<AdopterLoginResponse>(`${this.authUrl}/adopter/refresh`, {
        refreshToken,
      })
      .pipe(
        tap((response) => {
          this.setAuthData(response);
        }),
        catchError((error) => {
          this.clearAuthData();
          return throwError(() => error);
        })
      );
  }

  /**
   * Get current adopter profile
   */
  getCurrentProfile(): Observable<AdopterProfile> {
    return this.http.get<AdopterProfile>(`${this.apiUrl}/profile`).pipe(
      tap((profile) => {
        this.currentAdopterSubject.next(profile);
        localStorage.setItem('adopter_profile', JSON.stringify(profile));
      }),
      catchError(this.handleError)
    );
  }

  /**
   * Update adopter profile
   */
  updateProfile(
    request: UpdateAdopterProfileRequest
  ): Observable<AdopterProfile> {
    return this.http
      .put<AdopterProfile>(`${this.apiUrl}/profile`, request)
      .pipe(
        tap((profile) => {
          this.currentAdopterSubject.next(profile);
          localStorage.setItem('adopter_profile', JSON.stringify(profile));
        }),
        catchError(this.handleError)
      );
  }

  /**
   * Update adopter preferences
   */
  updatePreferences(
    preferences: AdopterPreferences
  ): Observable<AdopterPreferences> {
    return this.http
      .put<AdopterPreferences>(`${this.apiUrl}/preferences`, preferences)
      .pipe(
        tap((updatedPreferences) => {
          const currentAdopter = this.currentAdopterSubject.value;
          if (currentAdopter) {
            const updatedAdopter = {
              ...currentAdopter,
              preferences: updatedPreferences,
            };
            this.currentAdopterSubject.next(updatedAdopter);
            localStorage.setItem(
              'adopter_profile',
              JSON.stringify(updatedAdopter)
            );
          }
        }),
        catchError(this.handleError)
      );
  }

  /**
   * Change password
   */
  changePassword(
    request: ChangePasswordRequest
  ): Observable<{ message: string }> {
    return this.http
      .put<{ message: string }>(`${this.apiUrl}/change-password`, request)
      .pipe(catchError(this.handleError));
  }

  /**
   * Delete adopter account
   */
  deleteAccount(): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/account`).pipe(
      tap(() => {
        this.clearAuthData();
      }),
      catchError(this.handleError)
    );
  }

  /**
   * Get adoption history
   */
  getAdoptionHistory(): Observable<AdoptionRecord[]> {
    return this.http
      .get<AdoptionRecord[]>(`${this.apiUrl}/adoption-history`)
      .pipe(catchError(this.handleError));
  }

  /**
   * Verify email address
   */
  verifyEmail(token: string): Observable<{ message: string }> {
    return this.http
      .post<{ message: string }>(`${this.apiUrl}/verify-email`, { token })
      .pipe(catchError(this.handleError));
  }

  /**
   * Resend email verification
   */
  resendEmailVerification(): Observable<{ message: string }> {
    return this.http
      .post<{ message: string }>(`${this.apiUrl}/resend-verification`, {})
      .pipe(catchError(this.handleError));
  }

  /**
   * Request password reset
   */
  requestPasswordReset(email: string): Observable<{ message: string }> {
    return this.http
      .post<{ message: string }>(`${this.authUrl}/forgot-password`, { email })
      .pipe(catchError(this.handleError));
  }

  /**
   * Reset password using token
   */
  resetPassword(
    token: string,
    newPassword: string
  ): Observable<{ message: string }> {
    return this.http
      .post<{ message: string }>(`${this.authUrl}/reset-password`, {
        token,
        newPassword,
      })
      .pipe(catchError(this.handleError));
  }

  /**
   * Get adopters near a location (for admin/shelter use)
   */
  getAdoptersNearLocation(
    latitude: number,
    longitude: number,
    radius: number = 50
  ): Observable<AdopterProfile[]> {
    return this.http
      .get<AdopterProfile[]>(`${this.apiUrl}/near-location`, {
        params: {
          latitude: latitude.toString(),
          longitude: longitude.toString(),
          radius: radius.toString(),
        },
      })
      .pipe(catchError(this.handleError));
  }

  /**
   * Check if email is available during registration
   */
  checkEmailAvailability(email: string): Observable<{ available: boolean }> {
    return this.http
      .get<{ available: boolean }>(`${this.apiUrl}/check-email`, {
        params: { email },
      })
      .pipe(catchError(this.handleError));
  }

  /**
   * Upload profile picture
   */
  uploadProfilePicture(file: File): Observable<{ profilePictureUrl: string }> {
    const formData = new FormData();
    formData.append('profilePicture', file);

    return this.http
      .post<{ profilePictureUrl: string }>(
        `${this.apiUrl}/profile-picture`,
        formData
      )
      .pipe(
        tap((response) => {
          const currentAdopter = this.currentAdopterSubject.value;
          if (currentAdopter) {
            const updatedAdopter = {
              ...currentAdopter,
              profilePictureUrl: response.profilePictureUrl,
            };
            this.currentAdopterSubject.next(updatedAdopter);
            localStorage.setItem(
              'adopter_profile',
              JSON.stringify(updatedAdopter)
            );
          }
        }),
        catchError(this.handleError)
      );
  }

  /**
   * Set authentication data
   */
  private setAuthData(response: AdopterLoginResponse): void {
    localStorage.setItem('adopter_token', response.token);
    localStorage.setItem('adopter_refresh_token', response.refreshToken);
    localStorage.setItem('adopter_profile', JSON.stringify(response.adopter));
    localStorage.setItem(
      'adopter_token_expires',
      (Date.now() + response.expiresIn * 1000).toString()
    );

    this.currentAdopterSubject.next(response.adopter);
    this.isAuthenticatedSubject.next(true);
  }

  /**
   * Clear authentication data
   */
  private clearAuthData(): void {
    localStorage.removeItem('adopter_token');
    localStorage.removeItem('adopter_refresh_token');
    localStorage.removeItem('adopter_profile');
    localStorage.removeItem('adopter_token_expires');

    this.currentAdopterSubject.next(null);
    this.isAuthenticatedSubject.next(false);
  }

  /**
   * Get authentication token
   */
  getToken(): string | null {
    return localStorage.getItem('adopter_token');
  }

  /**
   * Check if token is expired
   */
  isTokenExpired(): boolean {
    const expiresAt = localStorage.getItem('adopter_token_expires');
    if (!expiresAt) return true;

    return Date.now() > parseInt(expiresAt);
  }

  /**
   * Get current adopter value (synchronous)
   */
  getCurrentAdopterValue(): AdopterProfile | null {
    return this.currentAdopterSubject.value;
  }

  /**
   * Check if user is authenticated (synchronous)
   */
  isAuthenticated(): boolean {
    return this.isAuthenticatedSubject.value;
  }

  /**
   * Handle HTTP errors
   */
  private handleError(error: any): Observable<never> {
    let errorMessage = 'An unexpected error occurred';

    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = error.error.message;
    } else {
      // Server-side error
      errorMessage =
        error.error?.message ||
        `Error Code: ${error.status}\nMessage: ${error.message}`;
    }

    console.error('AdoptersService Error:', error);
    return throwError(() => new Error(errorMessage));
  }

  /**
   * Get HTTP headers with authentication
   */
  private getAuthHeaders(): HttpHeaders {
    const token = this.getToken();
    return new HttpHeaders({
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    });
  }
}
