import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';

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
import { ImageModule } from 'primeng/image';
import { PetService } from '../../../shared/services/pet.service';
import {
  Pet,
  UpdatePetRequest,
  CreatePetRequest,
  PetStatus,
  PetsApi,
  PetMediaFileResponse,
  MediaFileType,
  PetSpeciesDto,
  PetBreedDto,
} from '../../../shared/apis/generated-apis';
import {
  PetMediaUploadComponent,
  MediaFile,
  MediaUploadData,
} from './pet-media-upload/pet-media-upload.component';

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
    ProgressSpinnerModule,
    ConfirmDialogModule,
    ImageModule,
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
  private petsApi = inject(PetsApi);

  petForm: FormGroup;
  pet = signal<Pet | null>(null);
  loading = signal(false);
  saving = signal(false);

  // Mode tracking
  isAddMode = signal(false);
  shelterId = signal<string | null>(null);

  // Image upload properties
  imagePreview = signal<string | null>(null);
  selectedImageFile = signal<File | null>(null);
  currentPetImageUrl = signal<string | null>(null);
  isUploadingImage = signal(false);

  // Media upload properties
  existingMediaImages = signal<MediaFile[]>([]);
  existingMediaVideos = signal<MediaFile[]>([]);
  existingMediaDocuments = signal<MediaFile[]>([]);
  currentMediaData = signal<MediaUploadData | null>(null);

  speciesOptions: { label: string; value: number }[] = [];
  breedOptions: { label: string; value: number }[] = [];
  isLoadingBreeds = false;

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
      speciesId: ['', Validators.required],
      breedId: [{ value: '', disabled: true }, Validators.required],
      dateOfBirth: ['', Validators.required],
      gender: ['', Validators.required],
      description: ['', [Validators.required, Validators.minLength(10)]],
      adoptionFee: [null, [Validators.required, Validators.min(0)]],
      weight: [null, [Validators.min(0.1)]],
      color: [''],
      isSpayedNeutered: [false],
      isVaccinated: [false],
      isMicrochipped: [false],
      isHouseTrained: [false],
      isGoodWithKids: [false],
      isGoodWithPets: [false],
      specialNeeds: [''],
      status: [PetStatus.Available, Validators.required],
    });

    // Subscribe to species changes to load breeds
    this.petForm
      .get('speciesId')
      ?.valueChanges.subscribe((speciesId: number) => {
        if (speciesId) {
          this.loadBreedsForSpecies(speciesId);
          // Reset breed selection when species changes
          this.petForm.get('breedId')?.setValue('');
          // Enable breed field when species is selected
          this.petForm.get('breedId')?.enable();
        } else {
          this.breedOptions = [];
          // Disable breed field when no species is selected
          this.petForm.get('breedId')?.disable();
        }
      });
  }

  ngOnInit() {
    this.loadSpeciesOptions();

    // Check query parameters for add mode
    const queryParams = this.route.snapshot.queryParams;
    const mode = queryParams['mode'];
    const shelterIdParam = queryParams['shelterId'];

    if (mode === 'add' && shelterIdParam) {
      // Add mode
      this.isAddMode.set(true);
      this.shelterId.set(shelterIdParam);
      // Initialize form with default values for add mode
      this.initializeAddMode();
    } else {
      // Edit mode
      const petId = this.route.snapshot.paramMap.get('id');
      if (petId) {
        this.isAddMode.set(false);
        this.loadPet(petId);
      } else {
        // No pet ID and not add mode, redirect to pets list
        window.location.href = '/pets';
      }
    }
  }

  private initializeAddMode() {
    // Set default values for add mode
    this.petForm.patchValue({
      status: PetStatus.Available,
      adoptionFee: null,
      isSpayedNeutered: false,
      isHouseTrained: false,
      isGoodWithKids: false,
      isGoodWithPets: false,
    });

    // Initialize empty media arrays
    this.existingMediaImages.set([]);
    this.existingMediaVideos.set([]);
    this.existingMediaDocuments.set([]);
  }

  private async loadPet(petId: string) {
    this.loading.set(true);
    try {
      const pet = await this.petService.getPetById(petId);
      if (pet) {
        this.pet.set(pet);
        this.populateForm(pet);
        await this.loadExistingMedia(petId);
      } else {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Pet not found',
        });

        window.location.href = '/pets';
      }
    } catch (error) {
      console.error('Error loading pet:', error);
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: 'Failed to load pet information',
      });
      window.location.href = '/pets';
    } finally {
      this.loading.set(false);
    }
  }

  private async loadSpeciesOptions() {
    try {
      const response = await firstValueFrom(this.petsApi.species());
      if (response.success && response.species) {
        this.speciesOptions = response.species.map(
          (species: PetSpeciesDto) => ({
            label: species.name || 'Unknown',
            value: species.speciesId || 0,
          }),
        );
      }
    } catch (error) {
      console.error('Failed to load species options:', error);
      // Fallback to hardcoded options if API fails
      this.speciesOptions = [
        { label: 'Dog', value: 1 },
        { label: 'Cat', value: 2 },
        { label: 'Rabbit', value: 3 },
        { label: 'Bird', value: 4 },
        { label: 'Other', value: 8 },
      ];
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
      }
    } catch (error) {
      console.error('Failed to load breed options:', error);
      // Fallback to generic options if API fails
      this.breedOptions = [
        { label: 'Mixed Breed', value: 0 },
        { label: 'Other', value: 0 },
      ];
    } finally {
      this.isLoadingBreeds = false;
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
      // Note: For edit mode, we'll need to map species/breed names to IDs
      // This will be handled by the form when species options are loaded
      dateOfBirth: dateOfBirth,
      gender: pet.gender,
      description: pet.description,
      adoptionFee: pet.adoptionFee,
      weight: pet.weight,
      color: pet.color || '',
      isSpayedNeutered: pet.isSpayedNeutered || false,
      isVaccinated: pet.isVaccinated || false,
      isMicrochipped: pet.isMicrochipped || false,
      isHouseTrained: pet.isHouseTrained || false,
      isGoodWithKids: pet.isGoodWithKids || false,
      isGoodWithPets: pet.isGoodWithPets || false,
      specialNeeds: pet.specialNeeds || '',
      status: pet.status || PetStatus.Available,
    });

    // Set species and breed after options are loaded
    if (pet.speciesId && pet.breedId) {
      this.setSpeciesAndBreed(pet.speciesId, pet.breedId);
    }

    // Load existing image
    this.loadExistingImage();
  }

  private async setSpeciesAndBreed(speciesId: number, breedId: number) {
    // Set the species directly by ID
    const speciesOption = this.speciesOptions.find(
      (s) => s.value === speciesId,
    );
    if (speciesOption) {
      this.petForm.get('speciesId')?.setValue(speciesOption.value);

      // Load breeds for this species
      await this.loadBreedsForSpecies(speciesOption.value);

      // Enable breed field and set breed by ID after breeds are loaded
      this.petForm.get('breedId')?.enable();
      const breedOption = this.breedOptions.find((b) => b.value === breedId);
      if (breedOption) {
        this.petForm.get('breedId')?.setValue(breedOption.value);
      }
    }
  }

  private buildPetRequest(
    formValue: Record<string, unknown>,
  ): CreatePetRequest | UpdatePetRequest {
    // Convert date to proper format
    const dateOfBirth = formValue['dateOfBirth'] as Date;
    const formattedDate = `${dateOfBirth.getFullYear()}-${String(dateOfBirth.getMonth() + 1).padStart(2, '0')}-${String(dateOfBirth.getDate()).padStart(2, '0')}`;

    // Build unified request object (both CreatePetRequest and UpdatePetRequest are now identical)
    const requestData = {
      name: formValue['name'] as string,
      speciesId: formValue['speciesId'] as number,
      breedId: formValue['breedId'] as number,
      dateOfBirth: formattedDate,
      gender: formValue['gender'] as string,
      description: formValue['description'] as string,
      adoptionFee: formValue['adoptionFee'] as number,
      weight: formValue['weight'] as number | undefined,
      color: (formValue['color'] as string) || '',
      isSpayedNeutered: (formValue['isSpayedNeutered'] as boolean) || false,
      isVaccinated: (formValue['isVaccinated'] as boolean) || false,
      isMicrochipped: (formValue['isMicrochipped'] as boolean) || false,
      isHouseTrained: (formValue['isHouseTrained'] as boolean) || false,
      isGoodWithKids: (formValue['isGoodWithKids'] as boolean) || false,
      isGoodWithPets: (formValue['isGoodWithPets'] as boolean) || false,
      specialNeeds: (formValue['specialNeeds'] as string) || '',
      status: (formValue['status'] as PetStatus) || PetStatus.Available,
    };

    return this.isAddMode()
      ? new CreatePetRequest(requestData)
      : new UpdatePetRequest(requestData);
  }

  async onSubmit() {
    if (this.petForm.valid) {
      // For add mode, we don't need a pet to exist yet
      if (!this.isAddMode() && !this.pet()) {
        return;
      }

      this.saving.set(true);
      this.isUploadingImage.set(!!this.selectedImageFile());

      try {
        const formValue = this.petForm.value;
        const request = this.buildPetRequest(formValue);

        if (this.isAddMode()) {
          const createdPet = await this.petService.createPetAndUploadImage(
            this.shelterId()!,
            request as CreatePetRequest,
            this.selectedImageFile() || undefined,
          );

          if (createdPet) {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Pet created successfully',
            });

            window.location.href = '/pets';
          } else {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to create pet',
            });
          }
        } else {
          const updatedPet = await this.petService.updatePetAndUploadImage(
            this.pet()!.petId!,
            request as UpdatePetRequest,
            this.selectedImageFile() || undefined,
          );

          if (updatedPet) {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Pet updated successfully',
            });

            window.location.href = '/pets';
          } else {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to update pet',
            });
          }
        }
      } catch (error) {
        console.error('Error saving pet:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: `Failed to ${this.isAddMode() ? 'create' : 'update'} pet`,
        });
      } finally {
        this.saving.set(false);
        this.isUploadingImage.set(false);
      }
    } else {
      this.markFormGroupTouched();
    }
  }

  onCancel() {
    window.location.href = '/pets';
  }

  onImageUpload(event: FileSelectEvent) {
    const file = event.files[0];
    if (file) {
      this.selectedImageFile.set(file);

      // Create preview
      const reader = new FileReader();
      reader.onload = (e) => {
        this.imagePreview.set(e.target?.result as string);
      };
      reader.readAsDataURL(file);
    }
  }

  onImageRemove() {
    // If there was a newly selected file, remove it and revert to current image
    if (this.selectedImageFile()) {
      this.selectedImageFile.set(null);
      this.imagePreview.set(this.currentPetImageUrl());
    } else {
      this.selectedImageFile.set(null);
      this.imagePreview.set(null);
    }
  }

  private async loadExistingImage() {
    const currentPet = this.pet();
    if (!currentPet?.petId || !currentPet.mainImageFileExtension) return;

    try {
      // Get the download URL for the existing image
      const downloadUrlRequest = {
        petRequests: [
          {
            petId: currentPet.petId,
            mainImageFileExtension: currentPet.mainImageFileExtension,
          },
        ],
      };

      const response =
        await this.petService.getPetImageDownloadUrls(downloadUrlRequest);
      if (response.success && response.petImageUrls[currentPet.petId]) {
        this.currentPetImageUrl.set(response.petImageUrls[currentPet.petId]);
        this.imagePreview.set(this.currentPetImageUrl());
      }
    } catch (error) {
      console.log('Could not load existing pet image:', error);
      // Don't show error to user as this is not critical
    }
  }

  private async loadExistingMedia(petId: string) {
    try {
      // Call the API to get existing media files
      const mediaResponse = await firstValueFrom(this.petsApi.mediaGET(petId));

      if (mediaResponse.success) {
        // Convert PetMediaFileResponse to MediaFile format for display
        const convertToMediaFile = (
          petMediaFile: PetMediaFileResponse,
        ): MediaFile => ({
          id: petMediaFile.mediaFileId,
          name: petMediaFile.fileName || '',
          url: petMediaFile.downloadUrl || '',
          type: this.convertMediaFileType(petMediaFile.fileType),
          size: petMediaFile.fileSizeBytes,
        });

        // Set the existing media arrays
        this.existingMediaImages.set(
          (mediaResponse.images || []).map(convertToMediaFile),
        );
        this.existingMediaVideos.set(
          (mediaResponse.videos || []).map(convertToMediaFile),
        );
        this.existingMediaDocuments.set(
          (mediaResponse.documents || []).map(convertToMediaFile),
        );

        console.log('Loaded existing media for pet:', petId, {
          images: this.existingMediaImages().length,
          videos: this.existingMediaVideos().length,
          documents: this.existingMediaDocuments().length,
        });
      } else {
        console.warn(
          'Failed to load existing media:',
          mediaResponse.errorMessage,
        );
        // Initialize with empty arrays on failure
        this.existingMediaImages.set([]);
        this.existingMediaVideos.set([]);
        this.existingMediaDocuments.set([]);
      }
    } catch (error) {
      console.log('Could not load existing pet media:', error);
      // Initialize with empty arrays on error
      this.existingMediaImages.set([]);
      this.existingMediaVideos.set([]);
      this.existingMediaDocuments.set([]);
      // Don't show error to user as this is not critical
    }
  }

  private convertMediaFileType(
    fileType: MediaFileType | undefined,
  ): 'image' | 'video' | 'document' {
    switch (fileType) {
      case MediaFileType.Image:
        return 'image';
      case MediaFileType.Video:
        return 'video';
      case MediaFileType.Document:
        return 'document';
      default:
        return 'document'; // Default fallback
    }
  }

  private markFormGroupTouched() {
    Object.keys(this.petForm.controls).forEach((key) => {
      const control = this.petForm.get(key);
      control?.markAsTouched();
    });
  }

  onMediaDataChange(mediaData: MediaUploadData) {
    this.currentMediaData.set(mediaData);
    console.log('Media data changed:', mediaData);
  }

  async onMediaUploadComplete() {
    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: 'Media files uploaded successfully',
    });

    // Reload the existing media data to reflect the new uploads
    const currentPet = this.pet();
    if (currentPet?.petId) {
      await this.loadExistingMedia(currentPet.petId);
    }
  }

  onDelete() {
    if (!this.pet()) return;

    this.confirmationService.confirm({
      message: `Are you sure you want to delete ${this.pet()!.name}? This action cannot be undone.`,
      header: 'Delete Pet',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: async () => {
        try {
          this.saving.set(true);
          await this.petService.deletePet(this.pet()!.petId!);

          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Pet deleted successfully',
          });

          // Navigate back to pets list
          window.location.href = '/pets';
        } catch (error) {
          console.error('Error deleting pet:', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to delete pet',
          });
        } finally {
          this.saving.set(false);
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
