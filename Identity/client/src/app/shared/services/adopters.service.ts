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

  constructor(private http: HttpClient) {}

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
}
