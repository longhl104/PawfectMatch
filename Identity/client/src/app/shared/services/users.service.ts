import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';

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

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
  message: string;
  data: TokenData | null;
}

@Injectable({
  providedIn: 'root',
})
export class UsersService {
  private http = inject(HttpClient);

  private readonly authUrl = `${environment.apiUrl}/auth`;

  /**
   * Login user
   */
  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.authUrl}/login`, request);
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
