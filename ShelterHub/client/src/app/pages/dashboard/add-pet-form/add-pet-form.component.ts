import { Component, EventEmitter, inject, Output } from '@angular/core';
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
import { DynamicDialogRef, DynamicDialogConfig } from 'primeng/dynamicdialog';
import { PetService } from 'shared/services/pet.service';
import { ToastService } from '@longhl104/pawfect-match-ng';
import { CreatePetRequest } from 'shared/apis/generated-apis';

@Component({
  selector: 'app-add-pet-form',
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
  ],
  templateUrl: './add-pet-form.component.html',
  styleUrl: './add-pet-form.component.scss',
})
export class AddPetFormComponent {
  @Output() petAdded = new EventEmitter<void>();
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
      age: [null, [Validators.required, Validators.min(0), Validators.max(30)]],
      gender: ['', Validators.required],
      description: [null, [Validators.required]],
    });
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

      const petData: CreatePetRequest = this.petForm.value;

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
      species: 'Species',
      breed: 'Breed',
      age: 'Age',
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
