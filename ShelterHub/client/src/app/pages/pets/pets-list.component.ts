import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { DataViewModule } from 'primeng/dataview';
import { InputTextModule } from 'primeng/inputtext';
import { SkeletonModule } from 'primeng/skeleton';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import {
  DynamicDialogModule,
  DialogService,
  DynamicDialogRef,
} from 'primeng/dynamicdialog';

import { PetService } from 'shared/services/pet.service';
import { AuthService } from 'shared/services/auth.service';
import { ToastService } from '@longhl104/pawfect-match-ng';
import {
  ShelterService,
  type ShelterInfo,
} from '../../shared/services/shelter.service';
import {
  Pet,
  PetsApi,
  GetPetImageDownloadUrlsRequest,
  PetImageDownloadUrlRequest,
  PetStatus,
} from 'shared/apis/generated-apis';
import { AddPetFormComponent } from '../dashboard/add-pet-form/add-pet-form.component';

interface FilterOption {
  label: string;
  value: string | null;
}

@Component({
  selector: 'app-pets-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    CardModule,
    TagModule,
    DataViewModule,
    InputTextModule,
    SkeletonModule,
    TooltipModule,
    ConfirmDialogModule,
    DynamicDialogModule,
  ],
  providers: [ConfirmationService],
  templateUrl: './pets-list.component.html',
  styleUrl: './pets-list.component.scss',
})
export class PetsListComponent implements OnInit {
  private shelterService = inject(ShelterService);
  private petService = inject(PetService);
  private petsApi = inject(PetsApi);
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private router = inject(Router);
  private dialogService = inject(DialogService);
  private confirmationService = inject(ConfirmationService);

  shelterInfo: ShelterInfo | null = null;
  allPets: Pet[] = [];
  filteredPets: Pet[] = [];
  petMainImageUrls = new Map<string, string>();
  isLoading = true;
  searchTerm = '';
  selectedStatus: string | null = null;
  selectedSpecies: string | null = null;

  private dialogRef?: DynamicDialogRef;

  statusOptions: FilterOption[] = [
    { label: 'All Statuses', value: null },
    { label: 'Available', value: PetStatus.Available },
    { label: 'Pending', value: PetStatus.Pending },
    { label: 'Adopted', value: PetStatus.Adopted },
    { label: 'Medical Hold', value: PetStatus.MedicalHold },
  ];

  speciesOptions: FilterOption[] = [
    { label: 'All Species', value: null },
    { label: 'Dog', value: 'Dog' },
    { label: 'Cat', value: 'Cat' },
    { label: 'Rabbit', value: 'Rabbit' },
    { label: 'Bird', value: 'Bird' },
    { label: 'Other', value: 'Other' },
  ];

  ngOnInit() {
    this.loadPetsData();
  }

  async loadPetsData() {
    try {
      this.isLoading = true;

      // Load shelter information
      this.shelterInfo = await this.shelterService.getShelterInfo();

      // Load all pets
      this.allPets = await this.petService.getAllPets(this.shelterInfo.shelterId);
      this.filteredPets = [...this.allPets];

      // Load pet images
      await this.loadPetImages();

      // Update species filter options based on actual data
      this.updateSpeciesOptions();
    } catch (error) {
      console.error('Error loading pets data:', error);
      this.toastService.error('Failed to load pets data');
    } finally {
      this.isLoading = false;
    }
  }

  private async loadPetImages() {
    const petIdsAndExtensions = this.allPets
      .map((pet) => ({
        petId: pet.petId!,
        mainImageFileExtension: pet.mainImageFileExtension,
      }))
      .filter((pet) => pet.mainImageFileExtension);

    if (petIdsAndExtensions.length > 0) {
      try {
        const petImagesResponse = await firstValueFrom(
          this.petsApi.downloadUrls(
            new GetPetImageDownloadUrlsRequest({
              petRequests: petIdsAndExtensions.map(
                (pe) =>
                  new PetImageDownloadUrlRequest({
                    petId: pe.petId,
                    mainImageFileExtension: pe.mainImageFileExtension,
                  }),
              ),
            }),
          ),
        );

        if (petImagesResponse.petImageUrls) {
          for (const [petId, url] of Object.entries(
            petImagesResponse.petImageUrls,
          )) {
            if (url) {
              this.petMainImageUrls.set(petId, url);
            }
          }
        }
      } catch (error) {
        console.error('Error loading pet images:', error);
        this.toastService.error('Failed to load pet images');
      }
    }
  }

  private updateSpeciesOptions() {
    const uniqueSpecies = [...new Set(this.allPets.map(pet => pet.species))].filter(Boolean);
    this.speciesOptions = [
      { label: 'All Species', value: null },
      ...uniqueSpecies.map(species => ({ label: species!, value: species! }))
    ];
  }

  applyFilters() {
    this.filteredPets = this.allPets.filter(pet => {
      const matchesSearch = !this.searchTerm ||
        (pet.name && pet.name.toLowerCase().includes(this.searchTerm.toLowerCase())) ||
        (pet.breed && pet.breed.toLowerCase().includes(this.searchTerm.toLowerCase())) ||
        (pet.description && pet.description.toLowerCase().includes(this.searchTerm.toLowerCase()));

      const matchesStatus = !this.selectedStatus || pet.status === this.selectedStatus;
      const matchesSpecies = !this.selectedSpecies || pet.species === this.selectedSpecies;

      return matchesSearch && matchesStatus && matchesSpecies;
    });
  }

  onSearchChange() {
    this.applyFilters();
  }

  onStatusChange() {
    this.applyFilters();
  }

  onSpeciesChange() {
    this.applyFilters();
  }

  clearFilters() {
    this.searchTerm = '';
    this.selectedStatus = null;
    this.selectedSpecies = null;
    this.filteredPets = [...this.allPets];
  }

  onAddPet() {
    if (!this.shelterInfo) {
      this.toastService.error('Shelter information is not available');
      return;
    }

    this.dialogRef = this.dialogService.open(AddPetFormComponent, {
      header: 'Add New Pet',
      width: '50vw',
      breakpoints: {
        '1199px': '75vw',
        '575px': '90vw',
      },
      modal: true,
      dismissableMask: true,
      data: {
        shelterId: this.shelterInfo.shelterId,
      },
    });

    this.dialogRef.onClose.subscribe((result) => {
      if (result) {
        this.loadPetsData(); // Refresh the data
      }
    });
  }

  onEditPet(_pet: Pet) {
    // TODO: Implement edit pet functionality
    this.toastService.info('Edit pet functionality coming soon');
  }

  onDeletePet(pet: Pet, event: Event) {
    event.stopPropagation();

    this.confirmationService.confirm({
      target: event.target as EventTarget,
      message: `Are you sure you want to delete ${pet.name}?`,
      header: 'Delete Pet',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger p-button-text',
      rejectButtonStyleClass: 'p-button-text',
      accept: async () => {
        try {
          await this.petService.deletePet(pet.petId!);
          this.toastService.success(`${pet.name} has been deleted`);
          this.loadPetsData(); // Refresh the data
        } catch (error) {
          console.error('Error deleting pet:', error);
          this.toastService.error('Failed to delete pet');
        }
      }
    });
  }

  onUpdateStatus(pet: Pet, newStatus: PetStatus, event: Event) {
    event.stopPropagation();

    this.confirmationService.confirm({
      target: event.target as EventTarget,
      message: `Update ${pet.name}'s status to ${newStatus}?`,
      header: 'Update Status',
      icon: 'pi pi-question-circle',
      accept: async () => {
        try {
          await this.petService.updatePetStatus(pet.petId!, newStatus);
          this.toastService.success(`${pet.name}'s status updated to ${newStatus}`);
          this.loadPetsData(); // Refresh the data
        } catch (error) {
          console.error('Error updating pet status:', error);
          this.toastService.error('Failed to update pet status');
        }
      }
    });
  }

  getStatusSeverity(status: string): 'success' | 'info' | 'warning' | 'danger' {
    switch (status) {
      case 'Available':
        return 'success';
      case 'Pending':
        return 'info';
      case 'Adopted':
        return 'warning';
      case 'MedicalHold':
        return 'danger';
      default:
        return 'info';
    }
  }

  onImageError(event: Event) {
    const target = event.target as HTMLImageElement;
    target.style.display = 'none';

    const parent = target.parentElement;
    if (parent) {
      parent.innerHTML =
        '<div class="w-full h-full bg-gray-100 flex align-items-center justify-content-center"><i class="pi pi-image text-4xl text-gray-400"></i></div>';
    }
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }

  getAvailableStatus(): PetStatus {
    return PetStatus.Available;
  }

  getPendingStatus(): PetStatus {
    return PetStatus.Pending;
  }

  getAdoptedStatus(): PetStatus {
    return PetStatus.Adopted;
  }
}
