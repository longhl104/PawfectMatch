import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { RegistrationApi, ShelterAdminRegistrationRequest as GeneratedShelterAdminRequest, IShelterAdminRegistrationRequest, IShelterAdminRegistrationResponse } from '../apis/generated-apis';

// Use the interface types for better compatibility
export type ShelterAdminRegistrationRequest = IShelterAdminRegistrationRequest;
export type ShelterAdminRegistrationResponse = IShelterAdminRegistrationResponse;

@Injectable({
  providedIn: 'root',
})
export class ShelterAdminService {
  private registrationApi = inject(RegistrationApi);

  /**
   * Register a new shelter admin
   */
  register(
    request: ShelterAdminRegistrationRequest,
  ): Observable<ShelterAdminRegistrationResponse> {
    // Convert the interface to the generated class
    const registrationRequest = new GeneratedShelterAdminRequest(request);
    return this.registrationApi.shelterAdmin(registrationRequest);
  }
}
