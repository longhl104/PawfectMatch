import { ChangeDetectionStrategy, Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { DividerModule } from 'primeng/divider';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { Pet } from '../browse/types/pet.interface';

@Component({
  selector: 'app-adoption-application',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    CardModule,
    InputTextModule,
    SelectModule,
    CheckboxModule,
    DividerModule,
    ToastModule,
  ],
  providers: [MessageService],
  templateUrl: './adoption-application.html',
  styleUrl: './adoption-application.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdoptionApplicationComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  protected router = inject(Router);
  private messageService = inject(MessageService);

  pet = signal<Pet | null>(null);
  isSubmitting = signal(false);

  applicationForm = this.fb.group({
    // Personal Information
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required, Validators.pattern(/^[+]?[1-9][\d]{0,15}$/)]],
    dateOfBirth: ['', Validators.required],

    // Address Information
    address: ['', Validators.required],
    city: ['', Validators.required],
    state: ['', Validators.required],
    postcode: ['', Validators.required],

    // Housing Information
    housingType: ['', Validators.required],
    isOwner: [false],
    hasYard: [false],
    yardType: [''],

    // Experience and Lifestyle
    previousPets: [''],
    currentPets: [''],
    hoursAlone: ['', Validators.required],
    activityLevel: ['', Validators.required],

    // Adoption Specific
    whyAdopt: ['', [Validators.required, Validators.minLength(50)]],
    vetReference: [''],

    // Agreement
    agreeToTerms: [false, Validators.requiredTrue],
    agreeToHomeVisit: [false],
  });

  housingOptions = [
    { label: 'House', value: 'house' },
    { label: 'Apartment', value: 'apartment' },
    { label: 'Townhouse', value: 'townhouse' },
    { label: 'Unit', value: 'unit' },
    { label: 'Other', value: 'other' },
  ];

  yardOptions = [
    { label: 'Fully Fenced', value: 'fully_fenced' },
    { label: 'Partially Fenced', value: 'partially_fenced' },
    { label: 'No Fence', value: 'no_fence' },
    { label: 'N/A', value: 'na' },
  ];

  hoursAloneOptions = [
    { label: '0-2 hours', value: '0-2' },
    { label: '3-4 hours', value: '3-4' },
    { label: '5-6 hours', value: '5-6' },
    { label: '7-8 hours', value: '7-8' },
    { label: '9+ hours', value: '9+' },
  ];

  activityLevelOptions = [
    { label: 'Low - Prefer calm pets', value: 'low' },
    { label: 'Moderate - Some daily activity', value: 'moderate' },
    { label: 'High - Very active lifestyle', value: 'high' },
  ];

  ngOnInit() {
    // Get pet data from navigation state
    const navigation = this.router.getCurrentNavigation();
    const petData = navigation?.extras?.state?.['pet'] as Pet;

    if (petData) {
      this.pet.set(petData);
    } else {
      // Check if we have a pet ID in route params (future enhancement)
      const petId = this.route.snapshot.paramMap.get('petId');
      if (petId) {
        // TODO: Fetch pet data from API using petId
        console.log('Fetch pet data for ID:', petId);
      } else {
        // No pet data available, redirect to browse
        this.router.navigate(['/browse']);
      }
    }
  }

  onSubmit() {
    if (this.applicationForm.valid) {
      this.isSubmitting.set(true);

      // Simulate API call
      setTimeout(() => {
        const pet = this.pet();
        console.log('Adoption application submitted for:', pet?.name);
        console.log('Application data:', this.applicationForm.value);

        this.messageService.add({
          severity: 'success',
          summary: 'Application Submitted',
          detail: `Your adoption application for ${pet?.name} has been submitted successfully. The shelter will contact you within 2-3 business days.`,
          life: 5000
        });

        this.isSubmitting.set(false);

        // Navigate back to pet detail or browse page
        setTimeout(() => {
          this.router.navigate(['/browse']);
        }, 2000);
      }, 2000);
    } else {
      this.messageService.add({
        severity: 'error',
        summary: 'Form Invalid',
        detail: 'Please fill in all required fields correctly.',
        life: 3000
      });

      // Mark all fields as touched to show validation errors
      this.applicationForm.markAllAsTouched();
    }
  }

  onCancel() {
    const pet = this.pet();
    if (pet) {
      this.router.navigate(['/pet-detail'], {
        state: { pet }
      });
    } else {
      this.router.navigate(['/browse']);
    }
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.applicationForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.applicationForm.get(fieldName);
    if (field?.errors) {
      if (field.errors['required']) return `${fieldName} is required`;
      if (field.errors['email']) return 'Please enter a valid email address';
      if (field.errors['minlength']) return `${fieldName} is too short`;
      if (field.errors['pattern']) return 'Please enter a valid phone number';
      if (field.errors['requiredTrue']) return 'You must agree to the terms';
    }
    return '';
  }
}
