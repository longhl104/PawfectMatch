import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';

export interface ShelterAdminRegistrationRequest {
  email: string;
  password: string;
  shelterName: string;
  shelterContactNumber: string;
  shelterAddress: string;
  shelterWebsiteUrl?: string;
  shelterAbn?: string;
  shelterDescription?: string;
}

export interface ShelterAdminRegistrationResponse {
  success: boolean;
  message: string;
  userId: string;
  redirectUrl?: string;
  data?: object;
}

@Injectable({
  providedIn: 'root',
})
export class ShelterAdminService {
  private http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiUrl}/registration`;

  /**
   * Register a new shelter admin
   */
  register(
    request: ShelterAdminRegistrationRequest,
  ): Observable<ShelterAdminRegistrationResponse> {
    return this.http.post<ShelterAdminRegistrationResponse>(
      `${this.apiUrl}/shelter-admin`,
      request,
    );
  }
}
