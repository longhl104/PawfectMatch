import {
  QueryShelterRequest,
  Shelter,
  SheltersApi,
  ShelterPetStatisticsResponse,
} from './../apis/generated-apis';
import { firstValueFrom } from 'rxjs';
import { inject, Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ShelterService {
  private sheltersApi = inject(SheltersApi);

  async getShelterInfo(): Promise<Shelter> {
    const response = await firstValueFrom(
      this.sheltersApi.query(
        new QueryShelterRequest({
          attributesToGet: [
            'ShelterName',
            'ShelterAddress',
            'ShelterContactNumber',
          ],
        }),
      ),
    );

    return response;
  }

  async getPetStatistics(): Promise<ShelterPetStatisticsResponse> {
    const response = await firstValueFrom(
      this.sheltersApi.petStatistics()
    );

    return response;
  }
}
