import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { EditorModule } from 'primeng/editor';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { CheckboxModule } from 'primeng/checkbox';
import { FileUploadModule, FileSelectEvent } from 'primeng/fileupload';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

import { PetService } from '../../../shared/services/pet.service';
import { Pet, UpdatePetRequest, PetStatus } from '../../../shared/apis/generated-apis';

@Component({
  selector: 'app-edit-pet',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    EditorModule,
    SelectModule,
    DatePickerModule,
    InputNumberModule,
    CheckboxModule,
    FileUploadModule,
    CardModule,
    ToastModule,
    ProgressSpinnerModule
  ],
  providers: [MessageService],
  templateUrl: './edit-pet.component.html',
  styleUrl: './edit-pet.component.scss'
})
export class EditPetComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private petService = inject(PetService);
  private messageService = inject(MessageService);

  petForm: FormGroup;
  pet: Pet | null = null;
  loading = false;
  saving = false;

  speciesOptions = [
    { label: 'Dog', value: 'Dog' },
    { label: 'Cat', value: 'Cat' },
    { label: 'Rabbit', value: 'Rabbit' },
    { label: 'Bird', value: 'Bird' },
    { label: 'Other', value: 'Other' }
  ];

  genderOptions = [
    { label: 'Male', value: 'Male' },
    { label: 'Female', value: 'Female' }
  ];

  statusOptions = [
    { label: 'Available', value: PetStatus.Available },
    { label: 'Pending', value: PetStatus.Pending },
    { label: 'Adopted', value: PetStatus.Adopted },
    { label: 'Medical Hold', value: PetStatus.MedicalHold }
  ];

  constructor() {
    this.petForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      species: ['', Validators.required],
      breed: ['', [Validators.required, Validators.minLength(2)]],
      dateOfBirth: ['', Validators.required],
      gender: ['', Validators.required],
      description: ['', [Validators.required, Validators.minLength(10)]],
      adoptionFee: [0, [Validators.required, Validators.min(0)]],
      weight: [null, [Validators.min(0.1)]],
      color: [''],
      isSpayedNeutered: [false],
      isHouseTrained: [false],
      isGoodWithKids: [false],
      isGoodWithPets: [false],
      specialNeeds: [''],
      status: [PetStatus.Available, Validators.required]
    });
  }

  ngOnInit() {
    const petId = this.route.snapshot.paramMap.get('id');
    if (petId) {
      this.loadPet(petId);
    } else {
      this.router.navigate(['/pets']);
    }
  }

  private async loadPet(petId: string) {
    this.loading = true;
    try {
      const pet = await this.petService.getPetById(petId);
      if (pet) {
        this.pet = pet;
        this.populateForm(this.pet);
      } else {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Pet not found'
        });
        this.router.navigate(['/pets']);
      }
    } catch (error) {
      console.error('Error loading pet:', error);
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: 'Failed to load pet information'
      });
      this.router.navigate(['/pets']);
    } finally {
      this.loading = false;
    }
  }

  private populateForm(pet: Pet) {
    // Handle date conversion safely
    let dateOfBirth = null;
    if (pet.dateOfBirth) {
      if (typeof pet.dateOfBirth === 'string') {
        dateOfBirth = new Date(pet.dateOfBirth + 'T00:00:00');
      } else {
        dateOfBirth = new Date(pet.dateOfBirth);
      }
    }

    this.petForm.patchValue({
      name: pet.name,
      species: pet.species,
      breed: pet.breed,
      dateOfBirth: dateOfBirth,
      gender: pet.gender,
      description: pet.description,
      adoptionFee: pet.adoptionFee || 0,
      weight: pet.weight || null,
      color: pet.color || '',
      isSpayedNeutered: pet.isSpayedNeutered || false,
      isHouseTrained: pet.isHouseTrained || false,
      isGoodWithKids: pet.isGoodWithKids || false,
      isGoodWithPets: pet.isGoodWithPets || false,
      specialNeeds: pet.specialNeeds || '',
      status: pet.status
    });
  }

  async onSubmit() {
    if (this.petForm.valid && this.pet) {
      this.saving = true;
      try {
        const formValue = this.petForm.value;

        // Convert date to proper format
        const dateOfBirth = formValue.dateOfBirth;
        const formattedDate = `${dateOfBirth.getFullYear()}-${String(dateOfBirth.getMonth() + 1).padStart(2, '0')}-${String(dateOfBirth.getDate()).padStart(2, '0')}`;

        const updateRequest = new UpdatePetRequest({
          name: formValue.name,
          species: formValue.species,
          breed: formValue.breed,
          dateOfBirth: formattedDate,
          gender: formValue.gender,
          description: formValue.description,
          adoptionFee: formValue.adoptionFee,
          weight: formValue.weight,
          color: formValue.color || '',
          isSpayedNeutered: formValue.isSpayedNeutered,
          isHouseTrained: formValue.isHouseTrained,
          isGoodWithKids: formValue.isGoodWithKids,
          isGoodWithPets: formValue.isGoodWithPets,
          specialNeeds: formValue.specialNeeds || '',
          status: formValue.status
        });

        const response = await this.petService.updatePet(this.pet.petId!, updateRequest);

        if (response) {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Pet updated successfully'
          });
          this.router.navigate(['/pets']);
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to update pet'
          });
        }
      } catch (error) {
        console.error('Error updating pet:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to update pet'
        });
      } finally {
        this.saving = false;
      }
    } else {
      this.markFormGroupTouched();
    }
  }

  onCancel() {
    this.router.navigate(['/pets']);
  }

  onImageUpload(event: FileSelectEvent) {
    // Handle image upload logic here
    console.log('Image upload:', event);
  }

  private markFormGroupTouched() {
    Object.keys(this.petForm.controls).forEach(key => {
      const control = this.petForm.get(key);
      control?.markAsTouched();
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.petForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.petForm.get(fieldName);
    if (field?.errors) {
      if (field.errors['required']) return `${fieldName} is required`;
      if (field.errors['minlength']) return `${fieldName} is too short`;
      if (field.errors['min']) return `${fieldName} must be greater than ${field.errors['min'].min}`;
    }
    return '';
  }
}
