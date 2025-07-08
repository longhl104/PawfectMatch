import { firstValueFrom } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from 'environments/environment';

export interface ShelterInfo {
  id: string;
  name: string;
  address: string;
  phone: string;
  email: string;
  capacity: number;
  currentPets: number;
}

@Injectable({
  providedIn: 'root',
})
export class ShelterService {
  private http = inject(HttpClient);

  async getShelterInfo(): Promise<ShelterInfo> {
    const response = await firstValueFrom(
      this.http.post<ShelterInfo>(
        `${environment.apiUrl}/api/shelters/my-shelter/query`,
        {
          attributesToGet: ['ShelterName'],
        },
        {
          withCredentials: true,
        },
      ),
    );

    return response;
  }

  async updateShelterInfo(
    shelterInfo: Partial<ShelterInfo>,
  ): Promise<ShelterInfo> {
    // TODO: Replace with actual API call
    console.log('Updating shelter info:', shelterInfo);
    return this.getShelterInfo();
  }
}
