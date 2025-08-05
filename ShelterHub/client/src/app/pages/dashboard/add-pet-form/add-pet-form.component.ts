import { Component, inject, output, OnInit } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { EditorModule } from 'primeng/editor';
import { ButtonModule } from 'primeng/button';
import { FileUploadModule } from 'primeng/fileupload';
import { DatePickerModule } from 'primeng/datepicker';
import { DynamicDialogRef, DynamicDialogConfig } from 'primeng/dynamicdialog';
import { PetService } from 'shared/services/pet.service';
import { ToastService } from '@longhl104/pawfect-match-ng';
import {
  CreatePetRequest,
  PetsApi,
  PetSpeciesDto,
  PetBreedDto,
} from 'shared/apis/generated-apis';
import { formatDateToLocalString } from 'shared/utils';

@Component({
  selector: 'app-add-pet-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    EditorModule,
    ButtonModule,
    FileUploadModule,
    DatePickerModule,
  ],
  templateUrl: './add-pet-form.component.html',
  styleUrl: './add-pet-form.component.scss',
})
export class AddPetFormComponent implements OnInit {
  readonly petAdded = output<void>();
  readonly cancelled = output<void>();

  private fb = inject(FormBuilder);
  private petService = inject(PetService);
  private toastService = inject(ToastService);
  private dialogRef = inject(DynamicDialogRef);
  private config = inject(DynamicDialogConfig);
  private petsApi = inject(PetsApi);

  petForm: FormGroup;
  isSubmitting = false;
  isUploadingImage = false;
  imagePreview: string | null = null;
  selectedImageFile: File | null = null;
  maxDate = new Date(); // For date picker max date

  speciesOptions: { label: string; value: number }[] = [];
  breedOptions: { label: string; value: number }[] = [];
  isLoadingBreeds = false;
  genderOptions = [
    { label: 'Male', value: 'Male' },
    { label: 'Female', value: 'Female' },
  ];

  constructor() {
    this.petForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      speciesId: ['', Validators.required],
      breedId: ['', Validators.required],
      dateOfBirth: [null, [Validators.required]],
      gender: ['', Validators.required],
      description: [null, [Validators.required]],
    });

    // Watch for species changes to load breeds
    this.petForm.get('speciesId')?.valueChanges.subscribe((speciesId) => {
      if (speciesId) {
        this.loadBreedsForSpecies(speciesId);
      } else {
        this.breedOptions = [];
      }
      // Reset breed selection when species changes
      this.petForm.get('breedId')?.setValue('');
    });
  }

  async ngOnInit() {
    await this.loadSpeciesOptions();
  }

  private async loadSpeciesOptions() {
    const response = await firstValueFrom(this.petsApi.species());
    if (response.success && response.species) {
      this.speciesOptions = response.species.map((species: PetSpeciesDto) => ({
        label: species.name!,
        value: species.speciesId!,
      }));
    }
  }

  private async loadBreedsForSpecies(speciesId: number) {
    try {
      this.isLoadingBreeds = true;
      const response = await firstValueFrom(this.petsApi.breeds(speciesId));
      if (response.success && response.breeds) {
        this.breedOptions = response.breeds.map((breed: PetBreedDto) => ({
          label: breed.name || 'Unknown',
          value: breed.breedId || 0,
        }));

        // Automatically select the first breed
        if (this.breedOptions.length > 0) {
          this.petForm.get('breedId')?.setValue(this.breedOptions[0].value);
        }
      }
    } catch (error) {
      console.error('Failed to load breed options:', error);
      // Fallback to generic options if API fails
      this.breedOptions = [
        { label: 'Mixed Breed', value: 0 },
        { label: 'Other', value: 0 },
      ];

      // Automatically select the first fallback breed
      if (this.breedOptions.length > 0) {
        this.petForm.get('breedId')?.setValue(this.breedOptions[0].value);
      }
    } finally {
      this.isLoadingBreeds = false;
    }
  }

  async onSubmit() {
    const shelterId = this.config.data?.shelterId;
    if (!shelterId) {
      this.toastService.error('Shelter ID is required.');
      return;
    }

    if (this.petForm.invalid) {
      this.markFormGroupTouched(this.petForm);
      return;
    }

    try {
      this.isSubmitting = true;
      this.isUploadingImage = !!this.selectedImageFile;

      const petData: CreatePetRequest = {
        ...this.petForm.value,
        dateOfBirth: formatDateToLocalString(this.petForm.value.dateOfBirth), // Convert Date to local date string
      };

      // Use the new upload method that handles S3 upload
      await this.petService.createPetAndUploadImage(
        shelterId,
        petData,
        this.selectedImageFile || undefined,
      );

      this.toastService.success('Pet added successfully!');

      this.dialogRef.close(true);
    } catch (error) {
      console.error('Error adding pet:', error);
      if (error instanceof Error) {
        this.toastService.error(`Failed to add pet: ${error.message}`);
      } else {
        this.toastService.error('Failed to add pet. Please try again later.');
      }
    } finally {
      this.isSubmitting = false;
      this.isUploadingImage = false;
    }
  }

  onCancel() {
    this.dialogRef.close(false);
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.petForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.petForm.get(fieldName);
    if (field?.errors) {
      if (field.errors['required']) {
        return `${this.getFieldLabel(fieldName)} is required`;
      }
      if (field.errors['minlength']) {
        return `${this.getFieldLabel(fieldName)} must be at least ${field.errors['minlength'].requiredLength} characters`;
      }
      if (field.errors['min']) {
        return `${this.getFieldLabel(fieldName)} must be at least ${field.errors['min'].min}`;
      }
      if (field.errors['max']) {
        return `${this.getFieldLabel(fieldName)} must be at most ${field.errors['max'].max}`;
      }
    }
    return '';
  }

  private getFieldLabel(fieldName: string): string {
    const labels: Record<string, string> = {
      name: 'Name',
      speciesId: 'Species',
      breedId: 'Breed',
      dateOfBirth: 'Date of Birth',
      gender: 'Gender',
      description: 'Description',
      imageS3Key: 'Image URL',
    };

    return labels[fieldName] || fieldName;
  }

  private markFormGroupTouched(formGroup: FormGroup) {
    Object.keys(formGroup.controls).forEach((key) => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }

  onImageSelect(event: { files: File[] }) {
    const file = event.files[0];
    if (file) {
      this.selectedImageFile = file;

      // Create preview
      const reader = new FileReader();
      reader.onload = (e) => {
        this.imagePreview = e.target?.result as string;
      };
      reader.readAsDataURL(file);
    }
  }

  onImageRemove() {
    this.selectedImageFile = null;
    this.imagePreview = null;
  }
}
