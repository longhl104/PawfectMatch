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

@Injectable({
  providedIn: 'root',
})
export class UsersService {
  private http = inject(HttpClient);

  private readonly authUrl = `${environment.apiUrl}/users`;

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
