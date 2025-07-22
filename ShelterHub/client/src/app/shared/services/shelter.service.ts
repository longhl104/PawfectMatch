import {
  QueryShelterRequest,
  Shelter,
  SheltersApi,
  ShelterPetStatisticsResponse,
} from './../apis/generated-apis';
import { BehaviorSubject, firstValueFrom } from 'rxjs';
import { inject, Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ShelterService {
  private sheltersApi = inject(SheltersApi);
  private shelterSubject = new BehaviorSubject<Shelter | null>(null);
  public shelter$ = this.shelterSubject.asObservable();

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

    this.shelterSubject.next(response);
    return response;
  }

  async getPetStatistics(): Promise<ShelterPetStatisticsResponse> {
    const response = await firstValueFrom(this.sheltersApi.petStatistics());

    return response;
  }
}
