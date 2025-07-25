import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService, UserProfile } from 'shared/services/auth.service';
import { Observable } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule, ButtonModule, CardModule, TagModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
})
export class HomeComponent {
  private authService = inject(AuthService);

  currentUser$: Observable<UserProfile | null> = this.authService.currentUser$;
  isAuthenticated$ = this.authService.authStatus$;
  featuredPets = [
    {
      id: 1,
      name: 'Buddy',
      breed: 'Golden Retriever',
      age: 3,
      image:
        'https://images.unsplash.com/photo-1552053831-71594a27632d?w=400&h=300&fit=crop',
      description: 'Friendly and energetic, loves playing fetch!',
    },
    {
      id: 2,
      name: 'Luna',
      breed: 'Border Collie',
      age: 2,
      image:
        'https://images.unsplash.com/photo-1551717743-49959800b1f6?w=400&h=300&fit=crop',
      description: 'Intelligent and loyal, great with kids!',
    },
    {
      id: 3,
      name: 'Max',
      breed: 'Labrador Mix',
      age: 4,
      image:
        'https://images.unsplash.com/photo-1543466835-00a7907e9de1?w=400&h=300&fit=crop',
      description: 'Gentle giant who loves cuddles and walks!',
    },
    {
      id: 4,
      name: 'Bella',
      breed: 'Beagle',
      age: 1,
      image:
        'https://images.unsplash.com/photo-1544717297-fa95b6ee9643?w=400&h=300&fit=crop',
      description: 'Playful puppy looking for an active family!',
    },
  ];

  adoptionSteps = [
    {
      step: 1,
      title: 'Browse Pets',
      description:
        'Explore our collection of adorable pets waiting for their forever homes.',
      icon: '🔍',
    },
    {
      step: 2,
      title: 'Find Your Match',
      description:
        'Use our matching system to find pets that fit your lifestyle and preferences.',
      icon: '💕',
    },
    {
      step: 3,
      title: 'Meet & Greet',
      description: 'Schedule a visit to meet your potential new family member.',
      icon: '🤝',
    },
    {
      step: 4,
      title: 'Adopt',
      description:
        'Complete the adoption process and welcome your new pet home!',
      icon: '🏠',
    },
  ];

  onFindMatch() {
    // Check if user is authenticated before proceeding
    if (this.authService.isAuthenticated()) {
      // Navigate to pet matching page
      console.log('Navigating to pet matching...');
      // TODO: Implement navigation to matching page
    } else {
      // Redirect to login
      this.authService.redirectToLogin();
    }
  }

  onBrowsePets() {
    // Browse pets doesn't require authentication
    console.log('Navigating to browse pets...');
    // TODO: Implement navigation to browse pets page
  }

  onViewPet(petId: number) {
    // Navigate to pet details page
    console.log('Viewing pet:', petId);
  }
}
