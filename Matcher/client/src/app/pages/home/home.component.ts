import {
  Component,
  inject,
  OnInit,
  AfterViewInit,
  ElementRef,
  viewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService, UserProfile } from 'shared/services/auth.service';
import { Observable } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { AvatarModule } from 'primeng/avatar';
import { gsap } from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';

// Register GSAP plugins
gsap.registerPlugin(ScrollTrigger);

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule, ButtonModule, CardModule, TagModule, AvatarModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
})
export class HomeComponent implements OnInit, AfterViewInit {
  private authService = inject(AuthService);
  readonly heroSection = viewChild.required<ElementRef>('heroSection');
  readonly featuredSection = viewChild.required<ElementRef>('featuredSection');
  readonly statsSection = viewChild.required<ElementRef>('statsSection');

  currentUser$: Observable<UserProfile | null> = this.authService.currentUser$;
  isAuthenticated$ = this.authService.authStatus$;
  featuredPets = [
    {
      id: 1,
      name: 'Buddy',
      breed: 'Golden Retriever',
      age: 3,
      image:
        '/photo-1552053831-71594a27632d?w=400&h=300&fit=crop',
      description: 'Friendly and energetic, loves playing fetch!',
    },
    {
      id: 2,
      name: 'Luna',
      breed: 'Border Collie',
      age: 2,
      image:
        '/photo-1551717743-49959800b1f6?w=400&h=300&fit=crop',
      description: 'Intelligent and loyal, great with kids!',
    },
    {
      id: 3,
      name: 'Max',
      breed: 'Labrador Mix',
      age: 4,
      image:
        '/photo-1543466835-00a7907e9de1?w=400&h=300&fit=crop',
      description: 'Gentle giant who loves cuddles and walks!',
    },
    {
      id: 4,
      name: 'Bella',
      breed: 'Beagle',
      age: 1,
      image:
        '/photo-1544717297-fa95b6ee9643?w=400&h=300&fit=crop',
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

  ngOnInit() {
    // Initialize component state
    console.log('Home component initialized with GSAP animations');
  }

  ngAfterViewInit() {
    this.initializeAnimations();
  }

  private initializeAnimations() {
    // Hero section entrance animation
    const heroTimeline = gsap.timeline();

    // Animate hero text elements
    heroTimeline
      .from('.hero-title', {
        duration: 1.2,
        y: 50,
        opacity: 0,
        ease: 'power3.out',
      })
      .from(
        '.hero-subtitle',
        {
          duration: 1,
          y: 30,
          opacity: 0,
          ease: 'power2.out',
        },
        '-=0.8',
      )
      .from(
        '.hero-buttons',
        {
          duration: 0.8,
          y: 20,
          opacity: 0,
          ease: 'power2.out',
        },
        '-=0.6',
      )
      .from(
        '.hero-image',
        {
          duration: 1.5,
          scale: 0.8,
          opacity: 0,
          ease: 'power3.out',
        },
        '-=1',
      );

    // Floating animation for hero image
    gsap.to('.hero-image img', {
      duration: 3,
      y: -10,
      ease: 'power1.inOut',
      yoyo: true,
      repeat: -1,
    });

    // Pet cards stagger animation on scroll
    gsap.from('.pet-card', {
      scrollTrigger: {
        trigger: '.featured-pets',
        start: 'top 80%',
        end: 'bottom 20%',
        toggleActions: 'play none none reverse',
      },
      duration: 0.8,
      y: 60,
      opacity: 0,
      stagger: 0.2,
      ease: 'power2.out',
    });

    // Statistics counter animation
    gsap.from('.stat-number', {
      scrollTrigger: {
        trigger: '.stats-section',
        start: 'top 80%',
        end: 'bottom 20%',
        toggleActions: 'play none none reverse',
      },
      duration: 2,
      textContent: 0,
      ease: 'power2.out',
      snap: { textContent: 1 },
      stagger: 0.3,
    });

    // Steps animation
    gsap.from('.step-card', {
      scrollTrigger: {
        trigger: '.how-it-works',
        start: 'top 80%',
        end: 'bottom 20%',
        toggleActions: 'play none none reverse',
      },
      duration: 0.8,
      y: 50,
      opacity: 0,
      rotation: 5,
      stagger: 0.2,
      ease: 'back.out(1.7)',
    });

    // CTA section dramatic entrance
    gsap.from('.cta-section', {
      scrollTrigger: {
        trigger: '.cta-section',
        start: 'top 90%',
        end: 'bottom 20%',
        toggleActions: 'play none none reverse',
      },
      duration: 1.2,
      scale: 0.9,
      opacity: 0,
      ease: 'power3.out',
    });

    // Add parallax effect to hero background
    gsap.to('.hero-bg', {
      scrollTrigger: {
        trigger: '.hero-section',
        start: 'top top',
        end: 'bottom top',
        scrub: 1,
      },
      y: -50,
      ease: 'none',
    });
  }

  onFindMatch(event?: Event) {
    // Add button click animation
    if (event?.target) {
      gsap.to(event.target, {
        duration: 0.1,
        scale: 0.95,
        yoyo: true,
        repeat: 1,
        ease: 'power2.inOut',
      });
    }

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

  onBrowsePets(event?: Event) {
    // Add button click animation
    if (event?.target) {
      gsap.to(event.target, {
        duration: 0.1,
        scale: 0.95,
        yoyo: true,
        repeat: 1,
        ease: 'power2.inOut',
      });
    }

    // Browse pets doesn't require authentication
    console.log('Navigating to browse pets...');
    // TODO: Implement navigation to browse pets page
  }

  onViewPet(petId: number) {
    // Navigate to pet details page
    console.log('Viewing pet:', petId);
  }

  onRegister() {
    // Redirect to Identity service for registration
    this.authService.redirectToLogin();
  }

  onLogin() {
    // Redirect to Identity service for login
    this.authService.redirectToLogin();
  }
}
