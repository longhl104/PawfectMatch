import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
  OnInit,
} from '@angular/core';
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
import { AuthService } from '../../shared/services/auth.service';

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
  private authService = inject(AuthService);

  pet = signal<Pet | null>(null);
  isSubmitting = signal(false);

  applicationForm = this.fb.group({
    // Personal Information
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    phone: [
      '',
      [Validators.required, Validators.pattern(/^[+]?[1-9][\d]{0,15}$/)],
    ],
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
    console.log('Adoption application ngOnInit called');

    // Prefill user information from AuthService
    this.prefillUserInformation();

    // Get petId from route parameters
    const petId = this.route.snapshot.paramMap.get('petId');

    if (petId) {
      console.log('Pet ID found in route parameters:', petId);
      // TODO: Fetch pet data from API using petId
      // For now, we'll use a placeholder until the API is implemented
      this.fetchPetById(petId);
    } else {
      // No petId parameter, redirect to browse
      console.log('No petId parameter found, redirecting to browse');
      this.router.navigate(['/browse']);
    }
  }

  private fetchPetById(petId: string) {
    // TODO: Replace this with actual API call to fetch pet data
    // For now, we'll create a placeholder pet or redirect to browse
    console.log('TODO: Fetch pet data from API for petId:', petId);

    // Placeholder implementation - in real app, this would be an HTTP call
    // For now, redirect to browse since we don't have the API endpoint yet
    this.messageService.add({
      severity: 'info',
      summary: 'Pet Loading',
      detail: 'Loading pet information...',
      life: 2000,
    });

    // Redirect to browse for now until API is implemented
    setTimeout(() => {
      this.router.navigate(['/browse']);
    }, 2000);
  }

  private prefillUserInformation() {
    const currentUser = this.authService.getCurrentUser();

    if (currentUser) {
      // Use firstName and lastName directly from the user
      const firstName = currentUser.firstName || '';
      const lastName = currentUser.lastName || '';

      // Prefill the form with user data
      this.applicationForm.patchValue({
        firstName: firstName,
        lastName: lastName,
        email: currentUser.email || '',
      });

      // Disable the prefilled fields
      this.applicationForm.get('firstName')?.disable();
      this.applicationForm.get('lastName')?.disable();
      this.applicationForm.get('email')?.disable();

      console.log(
        'User information prefilled and disabled for:',
        `${firstName} ${lastName}`.trim(),
      );
    } else {
      console.warn('No user information available to prefill');
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
          life: 5000,
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
        life: 3000,
      });

      // Mark all fields as touched to show validation errors
      this.applicationForm.markAllAsTouched();
    }
  }

  onCancel() {
    const petId = this.route.snapshot.paramMap.get('petId');
    if (petId) {
      this.router.navigate(['/pet-detail', petId]);
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
