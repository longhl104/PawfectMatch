import { Injectable, inject } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import {
  RegistrationApi,
  AdopterRegistrationRequest as GeneratedAdopterRequest,
  IAdopterRegistrationResponse,
} from '../apis/generated-apis';

// Keep the existing detailed interface, but use firstName and lastName instead of fullName
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

// For the API response, use the generated interface
export type AdopterRegistrationResponse = IAdopterRegistrationResponse;

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
  private registrationApi = inject(RegistrationApi);

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
  ): Observable<AdopterRegistrationResponse> {
    // Convert the interface to the generated class
    const registrationRequest = new GeneratedAdopterRequest(request);
    return this.registrationApi.adopter(registrationRequest);
    // Note: HTTP errors are now handled by the global error interceptor
  }
}
