import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import {
  AuthApi,
  LoginRequest as GeneratedLoginRequest,
  ILoginRequest,
  ILoginResponse,
} from '../apis/generated-apis';

export interface VerificationRequest {
  email: string;
  code: string;
  userType: 'adopter' | 'shelter';
}

export interface VerificationResponse {
  message: string;
  verified: boolean;
}

export interface ResendCodeRequest {
  email: string;
  userType: 'adopter' | 'shelter';
}

export interface TokenData {
  accessToken: string;
  idToken: string;
  refreshToken: string;
  expiresAt: string; // ISO date string
  user: {
    id: string;
    email: string;
    fullName: string;
    userType: 'adopter' | 'shelter_admin';
    verified: boolean;
  } | null;
}

// Use the interface types for better compatibility
export type LoginRequest = ILoginRequest;
export type LoginResponse = ILoginResponse;

@Injectable({
  providedIn: 'root',
})
export class UsersService {
  private http = inject(HttpClient);
  private authApi = inject(AuthApi);

  private readonly authUrl = `${environment.apiUrl}/api/auth`;

  /**
   * Login user
   */
  login(request: LoginRequest): Observable<LoginResponse> {
    // Convert the interface to the generated class
    const loginRequest = new GeneratedLoginRequest(request);
    return this.authApi.login(loginRequest);
  }

  /**
   * Verify registration code
   */
  verifyCode(request: VerificationRequest): Observable<VerificationResponse> {
    return this.http.post<VerificationResponse>(
      `${this.authUrl}/verify-code`,
      request,
    );
  }

  /**
   * Resend verification code
   */
  resendVerificationCode(
    request: ResendCodeRequest,
  ): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.authUrl}/resend-code`,
      request,
    );
  }
}
