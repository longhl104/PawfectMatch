import { Component, EventEmitter, inject, Output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
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
import { UpdatePetRequest, Pet } from 'shared/apis/generated-apis';
import { formatDateToLocalString } from 'shared/utils';

@Component({
  selector: 'app-edit-pet-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    EditorModule,
    ButtonModule,
    FileUploadModule,
    DatePickerModule,
  ],
  templateUrl: './edit-pet-form.component.html',
  styleUrl: './edit-pet-form.component.scss',
})
export class EditPetFormComponent implements OnInit {
  @Output() petUpdated = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  private fb = inject(FormBuilder);
  private petService = inject(PetService);
  private toastService = inject(ToastService);
  private dialogRef = inject(DynamicDialogRef);
  private config = inject(DynamicDialogConfig);

  petForm: FormGroup;
  isSubmitting = false;
  isUploadingImage = false;
  imagePreview: string | null = null;
  selectedImageFile: File | null = null;
  maxDate = new Date(); // For date picker max date
  pet: Pet | null = null;
  currentPetImageUrl: string | null = null; // Store the original pet image URL

  speciesOptions = [
    { label: 'Dog', value: 'Dog' },
    { label: 'Cat', value: 'Cat' },
    { label: 'Rabbit', value: 'Rabbit' },
    { label: 'Bird', value: 'Bird' },
    { label: 'Other', value: 'Other' },
  ];
  genderOptions = [
    { label: 'Male', value: 'Male' },
    { label: 'Female', value: 'Female' },
  ];

  constructor() {
    this.petForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      species: ['', Validators.required],
      breed: ['', [Validators.required, Validators.minLength(2)]],
      dateOfBirth: [null, [Validators.required]],
      gender: ['', Validators.required],
      description: [null, [Validators.required]],
    });
  }

  ngOnInit() {
    // Get the pet data from the dialog config
    this.pet = this.config.data?.pet;
    if (this.pet) {
      this.populateForm();
      this.loadExistingImage();
    }
  }

  private populateForm() {
    if (!this.pet) return;

    this.petForm.patchValue({
      name: this.pet.name,
      species: this.pet.species,
      breed: this.pet.breed,
      dateOfBirth: this.pet.dateOfBirth ? new Date(this.pet.dateOfBirth) : null,
      gender: this.pet.gender,
      description: this.pet.description,
    });
  }

  private async loadExistingImage() {
    if (!this.pet?.petId || !this.pet.mainImageFileExtension) return;

    try {
      // Get the download URL for the existing image
      const downloadUrlRequest = {
        petRequests: [
          {
            petId: this.pet.petId,
            mainImageFileExtension: this.pet.mainImageFileExtension,
          },
        ],
      };

      const response = await this.petService.getPetImageDownloadUrls(downloadUrlRequest);
      if (response.success && response.petImageUrls[this.pet.petId]) {
        this.currentPetImageUrl = response.petImageUrls[this.pet.petId];
        this.imagePreview = this.currentPetImageUrl;
      }
    } catch (error) {
      console.log('Could not load existing pet image:', error);
      // Don't show error to user as this is not critical
    }
  }

  async onSubmit() {
    if (!this.pet) {
      this.toastService.error('Pet data is required.');
      return;
    }

    if (this.petForm.invalid) {
      this.markFormGroupTouched(this.petForm);
      return;
    }

    try {
      this.isSubmitting = true;
      this.isUploadingImage = !!this.selectedImageFile;

      const petData: UpdatePetRequest = {
        ...this.petForm.value,
        dateOfBirth: formatDateToLocalString(
          this.petForm.value.dateOfBirth,
        ),
      };

      // Use the new upload method that handles S3 upload
      const updatedPet = await this.petService.updatePetAndUploadImage(
        this.pet.petId!,
        petData,
        this.selectedImageFile || undefined,
      );

      this.toastService.success(`${updatedPet.name} has been updated successfully!`);
      this.dialogRef.close(true);
    } catch (error: unknown) {
      console.error('Error updating pet:', error);
      if (error instanceof Error && error.message) {
        this.toastService.error(`Failed to update pet: ${error.message}`);
      } else {
        this.toastService.error('Failed to update pet. Please try again later.');
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
      species: 'Species',
      breed: 'Breed',
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
    // If there was a newly selected file, remove it and revert to current image
    if (this.selectedImageFile) {
      this.selectedImageFile = null;
      this.imagePreview = this.currentPetImageUrl;
    } else {
      this.selectedImageFile = null;
      this.imagePreview = null;
    }
  }
}
