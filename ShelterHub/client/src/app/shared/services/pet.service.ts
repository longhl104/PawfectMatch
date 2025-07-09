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
  createdAt: Date;
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

export interface PresignedUrlRequest {
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  shelterId: string;
}

export interface PresignedUrlResponse {
  success: boolean;
  presignedUrl?: string;
  s3Url?: string;
  key?: string;
  expiresAt?: Date;
  errorMessage?: string;
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
  private readonly mediaApiUrl = `${environment.apiUrl}/api/media`;
  private readonly http = inject(HttpClient);

  async getAllPets(shelterId: string): Promise<Pet[]> {
    try {
      const response = await firstValueFrom(
        this.http.get<GetPetsResponse>(`${this.apiUrl}/shelter/${shelterId}`),
      );
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

  async getPresignedUrl(
    request: PresignedUrlRequest,
  ): Promise<PresignedUrlResponse> {
    try {
      const response = await firstValueFrom(
        this.http.post<PresignedUrlResponse>(
          `${this.mediaApiUrl}/presigned-url`,
          request,
        ),
      );

      if (!response?.success) {
        throw new Error(
          response?.errorMessage || 'Failed to get presigned URL',
        );
      }

      return response;
    } catch (error) {
      console.error('Error getting presigned URL:', error);
      throw error;
    }
  }

  async uploadToS3(presignedUrl: string, file: File): Promise<void> {
    try {
      console.log('Uploading to S3:', {
        presignedUrl,
        fileName: file.name,
        fileType: file.type,
        fileSize: file.size,
      });

      const response = await fetch(presignedUrl, {
        method: 'PUT',
        body: file,
        headers: {
          'Content-Type': file.type,
        },
        mode: 'cors', // Explicitly set CORS mode
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('S3 Upload Error:', {
          status: response.status,
          statusText: response.statusText,
          errorText,
        });
        throw new Error(
          `Upload failed: ${response.status} ${response.statusText} - ${errorText}`,
        );
      }

      console.log('S3 Upload successful');
    } catch (error) {
      console.error('Error uploading to S3:', error);
      throw error;
    }
  }

  async uploadImageAndCreatePet(
    shelterId: string,
    petData: CreatePetRequest,
    imageFile?: File,
  ): Promise<Pet> {
    try {
      const finalPetData = { ...petData };

      // If image file is provided, upload it first
      if (imageFile) {
        const presignedRequest: PresignedUrlRequest = {
          fileName: imageFile.name,
          contentType: imageFile.type,
          fileSizeBytes: imageFile.size,
          shelterId: shelterId,
        };

        const presignedResponse = await this.getPresignedUrl(presignedRequest);

        if (!presignedResponse.presignedUrl || !presignedResponse.s3Url) {
          throw new Error('Failed to get presigned URL for image upload');
        }

        await this.uploadToS3(presignedResponse.presignedUrl, imageFile);

        // Add the S3 URL to the pet data
        finalPetData.imageUrl = presignedResponse.s3Url;
      }

      // Create the pet with the image URL
      return await this.createPet(shelterId, finalPetData);
    } catch (error) {
      console.error('Error uploading image and creating pet:', error);
      throw error;
    }
  }
}
