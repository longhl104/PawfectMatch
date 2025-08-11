import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { environment } from 'environments/environment';

export interface AdopterRegistrationRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phoneNumber?: string;
  address: string;
  addressDetails?: {
    streetNumber: string;
    streetName: string;
    suburb: string;
    city: string;
    state: string;
    postcode: string;
    country: string;
    formattedAddress: string;
    latitude?: number;
    longitude?: number;
  };
  bio?: string;
}

export interface AdopterProfile {
  id: string;
  firstName: string;
  lastName: string;
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
  firstName?: string;
  lastName?: string;
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
  private http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiUrl}/adopters`;

  private currentAdopterSubject = new BehaviorSubject<AdopterProfile | null>(
    null,
  );
  public currentAdopter$ = this.currentAdopterSubject.asObservable();

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  /**
   * Register a new adopter
   */
  register(
    request: AdopterRegistrationRequest,
  ): Observable<{ message: string; userId: string }> {
    return this.http.post<{ message: string; userId: string }>(
      `${this.apiUrl}/register`,
      request,
    );
    // Note: HTTP errors are now handled by the global error interceptor
  }
}
