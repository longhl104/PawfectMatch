import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { PetCardComponent } from '../../components/pet-card/pet-card.component';
import { ApplicationSectionComponent } from '../../components/application-section/application-section.component';

export interface Pet {
  id: string;
  name: string;
  species: string;
  breed: string;
  age: number;
  gender: string;
  description: string;
  imageUrl: string;
  status: 'available' | 'pending' | 'adopted';
  dateAdded: Date;
  vaccinated: boolean;
  spayedNeutered: boolean;
  houseTrained: boolean;
  goodWithKids: boolean;
  goodWithPets: boolean;
}

export interface Application {
  id: string;
  petId: string;
  petName: string;
  applicantName: string;
  applicantEmail: string;
  status: 'pending' | 'approved' | 'rejected';
  submittedDate: Date;
  notes?: string;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, PetCardComponent, ApplicationSectionComponent],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  pets: Pet[] = [];
  recentApplications: Application[] = [];
  isLoading = false;

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.loadPets();
    this.loadRecentApplications();
  }

  loadPets(): void {
    this.isLoading = true;
    // Mock data - replace with actual service call
    setTimeout(() => {
      this.pets = [
        {
          id: '1',
          name: 'Buddy',
          species: 'Dog',
          breed: 'Golden Retriever',
          age: 3,
          gender: 'Male',
          description: 'Friendly and energetic dog looking for an active family.',
          imageUrl: 'https://images.unsplash.com/photo-1552053831-71594a27632d?w=300&h=300&fit=crop',
          status: 'available',
          dateAdded: new Date('2024-12-01'),
          vaccinated: true,
          spayedNeutered: true,
          houseTrained: true,
          goodWithKids: true,
          goodWithPets: true
        },
        {
          id: '2',
          name: 'Luna',
          species: 'Cat',
          breed: 'Siamese',
          age: 2,
          gender: 'Female',
          description: 'Calm and affectionate cat who loves to cuddle.',
          imageUrl: 'https://images.unsplash.com/photo-1514888286974-6c03e2ca1dba?w=300&h=300&fit=crop',
          status: 'pending',
          dateAdded: new Date('2024-11-28'),
          vaccinated: true,
          spayedNeutered: true,
          houseTrained: true,
          goodWithKids: true,
          goodWithPets: false
        },
        {
          id: '3',
          name: 'Max',
          species: 'Dog',
          breed: 'German Shepherd',
          age: 5,
          gender: 'Male',
          description: 'Loyal and protective dog, great for experienced owners.',
          imageUrl: 'https://images.unsplash.com/photo-1551717743-49959800b1f6?w=300&h=300&fit=crop',
          status: 'adopted',
          dateAdded: new Date('2024-11-20'),
          vaccinated: true,
          spayedNeutered: true,
          houseTrained: true,
          goodWithKids: false,
          goodWithPets: true
        },
        {
          id: '4',
          name: 'Whiskers',
          species: 'Cat',
          breed: 'Maine Coon',
          age: 1,
          gender: 'Male',
          description: 'Playful kitten with lots of personality.',
          imageUrl: 'https://images.unsplash.com/photo-1574144611937-0df059b5ef3e?w=300&h=300&fit=crop',
          status: 'available',
          dateAdded: new Date('2024-12-03'),
          vaccinated: true,
          spayedNeutered: false,
          houseTrained: false,
          goodWithKids: true,
          goodWithPets: true
        }
      ];
      this.isLoading = false;
    }, 1000);
  }

  loadRecentApplications(): void {
    // Mock data - replace with actual service call
    setTimeout(() => {
      this.recentApplications = [
        {
          id: '1',
          petId: '2',
          petName: 'Luna',
          applicantName: 'Sarah Johnson',
          applicantEmail: 'sarah.johnson@email.com',
          status: 'pending',
          submittedDate: new Date('2024-12-05'),
          notes: 'Has experience with cats and lives in a pet-friendly apartment.'
        },
        {
          id: '2',
          petId: '1',
          petName: 'Buddy',
          applicantName: 'Mike Davis',
          applicantEmail: 'mike.davis@email.com',
          status: 'approved',
          submittedDate: new Date('2024-12-04'),
          notes: 'Family with children and a large backyard.'
        },
        {
          id: '3',
          petId: '4',
          petName: 'Whiskers',
          applicantName: 'Emily Chen',
          applicantEmail: 'emily.chen@email.com',
          status: 'pending',
          submittedDate: new Date('2024-12-03'),
          notes: 'First-time cat owner but very eager to learn.'
        }
      ];
    }, 500);
  }

  onAddPet(): void {
    this.router.navigate(['/add-pet']);
  }

  onRunMatching(): void {
    this.isLoading = true;
    // Simulate matching process
    setTimeout(() => {
      this.isLoading = false;
      // Show success message or navigate to results
      console.log('Matching algorithm completed!');
    }, 2000);
  }

  onPetStatusChange(petId: string, newStatus: Pet['status']): void {
    const pet = this.pets.find(p => p.id === petId);
    if (pet) {
      pet.status = newStatus;
    }
  }

  onApplicationStatusChange(applicationId: string, newStatus: Application['status']): void {
    const application = this.recentApplications.find(a => a.id === applicationId);
    if (application) {
      application.status = newStatus;
    }
  }

  getPetCountByStatus(status: Pet['status']): number {
    return this.pets.filter(pet => pet.status === status).length;
  }

  trackByPetId(index: number, pet: Pet): string {
    return pet.id;
  }

  trackByApplicationId(index: number, application: Application): string {
    return application.id;
  }
}
