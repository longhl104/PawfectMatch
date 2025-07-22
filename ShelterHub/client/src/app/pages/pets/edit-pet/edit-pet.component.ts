import { Component, OnInit, inject } from '@angular/core';

import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
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
import { MessageService, ConfirmationService } from 'primeng/api';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { PetService } from '../../../shared/services/pet.service';
import { Pet, UpdatePetRequest, PetStatus } from '../../../shared/apis/generated-apis';
import { PetMediaUploadComponent, MediaFile, MediaUploadData } from './pet-media-upload/pet-media-upload.component';

@Component({
  selector: 'app-edit-pet',
  standalone: true,
  imports: [
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
    ProgressSpinnerModule,
    ConfirmDialogModule,
    PetMediaUploadComponent,
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './edit-pet.component.html',
  styleUrl: './edit-pet.component.scss',
})
export class EditPetComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private petService = inject(PetService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);

  petForm: FormGroup;
  pet: Pet | null = null;
  loading = false;
  saving = false;

  // Image upload properties
  imagePreview: string | null = null;
  selectedImageFile: File | null = null;
  currentPetImageUrl: string | null = null;
  isUploadingImage = false;

  // Media upload properties
  existingMediaImages: MediaFile[] = [];
  existingMediaVideos: MediaFile[] = [];
  existingMediaDocuments: MediaFile[] = [];
  currentMediaData: MediaUploadData | null = null;

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

  statusOptions = [
    { label: 'Available', value: PetStatus.Available },
    { label: 'Pending', value: PetStatus.Pending },
    { label: 'Adopted', value: PetStatus.Adopted },
    { label: 'Medical Hold', value: PetStatus.MedicalHold },
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
      status: [PetStatus.Available, Validators.required],
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
        await this.loadExistingMedia(petId);
      } else {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Pet not found',
        });
        this.router.navigate(['/pets']);
      }
    } catch (error) {
      console.error('Error loading pet:', error);
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: 'Failed to load pet information',
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
      weight: pet.weight,
      color: pet.color || '',
      isSpayedNeutered: pet.isSpayedNeutered || false,
      isHouseTrained: pet.isHouseTrained || false,
      isGoodWithKids: pet.isGoodWithKids || false,
      isGoodWithPets: pet.isGoodWithPets || false,
      specialNeeds: pet.specialNeeds || '',
      status: pet.status || PetStatus.Available,
    });

    // Load existing image
    this.loadExistingImage();
  }

  async onSubmit() {
    if (this.petForm.valid && this.pet) {
      this.saving = true;
      this.isUploadingImage = !!this.selectedImageFile;

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
          status: formValue.status,
        });

        // Use the new upload method that handles S3 upload
        const updatedPet = await this.petService.updatePetAndUploadImage(
          this.pet.petId!,
          updateRequest,
          this.selectedImageFile || undefined,
        );

        if (updatedPet) {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Pet updated successfully',
          });
          this.router.navigate(['/pets']);
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to update pet',
          });
        }
      } catch (error) {
        console.error('Error updating pet:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to update pet',
        });
      } finally {
        this.saving = false;
        this.isUploadingImage = false;
      }
    } else {
      this.markFormGroupTouched();
    }
  }

  onCancel() {
    this.router.navigate(['/pets']);
  }

  onImageUpload(event: FileSelectEvent) {
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

      const response =
        await this.petService.getPetImageDownloadUrls(downloadUrlRequest);
      if (response.success && response.petImageUrls[this.pet.petId]) {
        this.currentPetImageUrl = response.petImageUrls[this.pet.petId];
        this.imagePreview = this.currentPetImageUrl;
      }
    } catch (error) {
      console.log('Could not load existing pet image:', error);
      // Don't show error to user as this is not critical
    }
  }

  private async loadExistingMedia(petId: string) {
    try {
      // For now, initialize with empty arrays
      // In a real implementation, you would call an API to get existing media files
      this.existingMediaImages = [];
      this.existingMediaVideos = [];
      this.existingMediaDocuments = [];

      // Example of how you might load existing media:
      // const mediaResponse = await this.petService.getPetMedia(petId);
      // this.existingMediaImages = mediaResponse.images || [];
      // this.existingMediaVideos = mediaResponse.videos || [];
      // this.existingMediaDocuments = mediaResponse.documents || [];

      console.log('Loaded existing media for pet:', petId);
    } catch (error) {
      console.log('Could not load existing pet media:', error);
      // Don't show error to user as this is not critical
    }
  }

  private markFormGroupTouched() {
    Object.keys(this.petForm.controls).forEach((key) => {
      const control = this.petForm.get(key);
      control?.markAsTouched();
    });
  }

  onMediaDataChange(mediaData: MediaUploadData) {
    this.currentMediaData = mediaData;
    console.log('Media data changed:', mediaData);
  }

  onMediaUploadComplete() {
    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: 'Media files uploaded successfully'
    });

    // Optionally reload media data or refresh the component
    // This could involve calling an API to get updated media file information
  }

  onDelete() {
    if (!this.pet) return;

    this.confirmationService.confirm({
      message: `Are you sure you want to delete ${this.pet.name}? This action cannot be undone.`,
      header: 'Delete Pet',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: async () => {
        try {
          this.saving = true;
          await this.petService.deletePet(this.pet!.petId!);

          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Pet deleted successfully',
          });

          // Navigate back to pets list
          this.router.navigate(['/pets']);
        } catch (error) {
          console.error('Error deleting pet:', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to delete pet',
          });
        } finally {
          this.saving = false;
        }
      },
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
      if (field.errors['min'])
        return `${fieldName} must be greater than ${field.errors['min'].min}`;
    }
    return '';
  }
}
