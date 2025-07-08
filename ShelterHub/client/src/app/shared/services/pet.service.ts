import { Injectable } from '@angular/core';

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

@Injectable({
  providedIn: 'root'
})
export class PetService {

  async getAllPets(): Promise<Pet[]> {
    // TODO: Replace with actual API call
    return Promise.resolve([
      {
        id: '1',
        name: 'Buddy',
        species: 'Dog',
        breed: 'Golden Retriever',
        age: 3,
        gender: 'Male',
        status: 'available',
        description: 'Friendly and energetic dog looking for a loving family.',
        dateAdded: new Date('2024-12-01'),
        imageUrl: 'https://images.unsplash.com/photo-1552053831-71594a27632d?w=300&h=200&fit=crop&crop=faces'
      },
      {
        id: '2',
        name: 'Whiskers',
        species: 'Cat',
        breed: 'Siamese',
        age: 2,
        gender: 'Female',
        status: 'pending',
        description: 'Calm and affectionate cat who loves to cuddle.',
        dateAdded: new Date('2024-11-15'),
        imageUrl: 'https://images.unsplash.com/photo-1514888286974-6c03e2ca1dba?w=300&h=200&fit=crop&crop=faces'
      },
      {
        id: '3',
        name: 'Charlie',
        species: 'Dog',
        breed: 'Beagle',
        age: 5,
        gender: 'Male',
        status: 'medical_hold',
        description: 'Sweet beagle recovering from minor surgery.',
        dateAdded: new Date('2024-11-20'),
        imageUrl: 'https://images.unsplash.com/photo-1551717743-49959800b1f6?w=300&h=200&fit=crop&crop=faces'
      },
      {
        id: '4',
        name: 'Luna',
        species: 'Cat',
        breed: 'Persian',
        age: 4,
        gender: 'Female',
        status: 'available',
        description: 'Beautiful Persian cat with a gentle personality.',
        dateAdded: new Date('2024-12-10'),
        imageUrl: 'https://images.unsplash.com/photo-1596854407944-bf87f6fdd49e?w=300&h=200&fit=crop&crop=faces'
      }
    ]);
  }

  async getPetById(id: string): Promise<Pet | null> {
    const pets = await this.getAllPets();
    return pets.find(pet => pet.id === id) || null;
  }

  async createPet(petData: CreatePetRequest): Promise<Pet> {
    // TODO: Replace with actual API call
    const newPet: Pet = {
      id: Date.now().toString(),
      ...petData,
      status: 'available',
      dateAdded: new Date()
    };
    console.log('Creating new pet:', newPet);
    return Promise.resolve(newPet);
  }

  async updatePetStatus(petId: string, status: Pet['status']): Promise<Pet> {
    // TODO: Replace with actual API call
    const pet = await this.getPetById(petId);
    if (!pet) {
      throw new Error('Pet not found');
    }
    pet.status = status;
    console.log('Updating pet status:', pet);
    return Promise.resolve(pet);
  }

  async deletePet(petId: string): Promise<void> {
    // TODO: Replace with actual API call
    console.log('Deleting pet:', petId);
    return Promise.resolve();
  }
}
