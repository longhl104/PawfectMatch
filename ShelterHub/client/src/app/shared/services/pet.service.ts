import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from 'environments/environment';

export interface Pet {
  id: string;
  name: string;
  species: string;
  breed: string;
  age: number;
  gender: string;
  status: 'available' | 'pending' | 'adopted' | 'medical_hold';
  imageUrl?: string;
  description: string;
  dateAdded: Date;
  shelterId: string;
}

export interface CreatePetRequest {
  name: string;
  species: string;
  breed: string;
  age: number;
  gender: string;
  description: string;
  imageUrl?: string;
}

export interface PetResponse {
  success: boolean;
  pet?: Pet;
  errorMessage?: string;
}

export interface GetPetsResponse {
  success: boolean;
  pets?: Pet[];
  errorMessage?: string;
}

@Injectable({
  providedIn: 'root',
})
export class PetService {
  private readonly apiUrl = `${environment.apiUrl}/api/pets`;
  private readonly http = inject(HttpClient);

  async getAllPets(): Promise<Pet[]> {
    // TODO: Replace with actual shelter ID from auth context
    const shelterId = 'shelter-1'; // This should come from authentication context

    try {
      const response = await this.http
        .get<GetPetsResponse>(`${this.apiUrl}/shelter/${shelterId}`)
        .toPromise();
      return response?.pets || [];
    } catch (error) {
      console.error('Error fetching pets:', error);
      return [];
    }
  }

  async getPetById(id: string): Promise<Pet | null> {
    try {
      const response = await this.http
        .get<PetResponse>(`${this.apiUrl}/${id}`)
        .toPromise();
      return response?.pet || null;
    } catch (error) {
      console.error('Error fetching pet:', error);
      return null;
    }
  }

  async createPet(shelterId: string, petData: CreatePetRequest): Promise<Pet> {
    try {
      const response = await firstValueFrom(
        this.http.post<PetResponse>(
          `${this.apiUrl}/shelter/${shelterId}`,
          petData,
        ),
      );

      if (!response?.success || !response.pet) {
        throw new Error(response?.errorMessage || 'Failed to create pet');
      }

      return response.pet;
    } catch (error) {
      console.error('Error creating pet:', error);
      throw error;
    }
  }

  async updatePetStatus(petId: string, status: Pet['status']): Promise<Pet> {
    try {
      const response = await this.http
        .put<PetResponse>(`${this.apiUrl}/${petId}/status`, status)
        .toPromise();

      if (!response?.success || !response.pet) {
        throw new Error(
          response?.errorMessage || 'Failed to update pet status',
        );
      }

      return response.pet;
    } catch (error) {
      console.error('Error updating pet status:', error);
      throw error;
    }
  }

  async deletePet(petId: string): Promise<void> {
    try {
      const response = await this.http
        .delete<PetResponse>(`${this.apiUrl}/${petId}`)
        .toPromise();

      if (!response?.success) {
        throw new Error(response?.errorMessage || 'Failed to delete pet');
      }
    } catch (error) {
      console.error('Error deleting pet:', error);
      throw error;
    }
  }
}
