import { Component, inject, OnInit, OnDestroy, signal } from '@angular/core';
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
import { PaginatorModule } from 'primeng/paginator';
import { ConfirmationService } from 'primeng/api';
import type { PaginatorState } from 'primeng/paginator';
import {
  DynamicDialogModule,
  DialogService,
  DynamicDialogRef,
} from 'primeng/dynamicdialog';

import { PetService } from 'shared/services/pet.service';
import { ToastService } from '@longhl104/pawfect-match-ng';
import { ShelterService } from 'shared/services/shelter.service';
import {
  Pet,
  PetsApi,
  GetPetImageDownloadUrlsRequest,
  PetImageDownloadUrlRequest,
  PetStatus,
  GetPaginatedPetsResponse,
  Shelter,
} from 'shared/apis/generated-apis';
import { AddPetFormComponent } from '../dashboard/add-pet-form/add-pet-form.component';
import { getAgeLabel } from 'shared/utils';

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
    PaginatorModule,
    DynamicDialogModule,
  ],
  providers: [ConfirmationService],
  templateUrl: './pets-list.component.html',
  styleUrl: './pets-list.component.scss',
})
export class PetsListComponent implements OnInit, OnDestroy {
  private shelterService = inject(ShelterService);
  private petService = inject(PetService);
  private petsApi = inject(PetsApi);
  private toastService = inject(ToastService);
  private router = inject(Router);
  private dialogService = inject(DialogService);
  private confirmationService = inject(ConfirmationService);

  shelterInfo: Shelter | null = null;
  allPets = signal<Pet[]>([]);
  petMainImageUrls = new Map<string, string>();
  isLoading = true;
  searchName = '';
  searchBreed = '';
  selectedStatus: string | null = null;
  selectedSpecies: string | null = null;

  // Pagination properties
  currentPage = 0;
  pageSize = 12;
  totalRecords = 0;
  nextToken: string | null = null;
  pageTokens = new Map<number, string>(); // Store tokens for each page

  private dialogRef?: DynamicDialogRef;
  private searchDebounceTimer?: NodeJS.Timeout;

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

  ngOnDestroy() {
    // Clean up the debounce timer
    if (this.searchDebounceTimer) {
      clearTimeout(this.searchDebounceTimer);
    }
  }

  async loadPetsData(page = 0) {
    try {
      this.isLoading = true;

      // Load shelter information if not already loaded
      if (!this.shelterInfo) {
        this.shelterInfo = await this.shelterService.getShelterInfo();
      }

      // Calculate which token to use for this page
      let tokenToUse: string | undefined = undefined;
      if (page > 0) {
        tokenToUse = this.pageTokens.get(page) || undefined;
      }

      // Parse search term into name and breed components
      let nameFilter: string | undefined = undefined;
      let breedFilter: string | undefined = undefined;

      if (this.searchName.trim()) {
        nameFilter = this.searchName.trim();
      }

      if (this.searchBreed.trim()) {
        breedFilter = this.searchBreed.trim();
      }

      // Load paginated pets with server-side filtering
      const response: GetPaginatedPetsResponse = await firstValueFrom(
        this.petsApi.paginated(
          this.shelterInfo.shelterId,
          this.pageSize,
          tokenToUse,
          (this.selectedStatus as PetStatus | undefined) ?? undefined,
          (this.selectedSpecies as string | undefined) ?? undefined,
          nameFilter,
          breedFilter,
        ),
      );

      if (!response.success) {
        throw new Error(response.errorMessage || 'Failed to load pets');
      }

      this.allPets.set(response.pets || []);
      this.totalRecords = response.totalCount || 0;
      this.currentPage = page;

      // Store the next token for the next page
      if (response.nextToken) {
        this.pageTokens.set(page + 1, response.nextToken);
      }

      // Load pet images for current page
      await this.loadPetImages();

      // Update species filter options based on current data
      this.updateSpeciesOptions();
    } catch (error) {
      console.error('Error loading pets data:', error);
      this.toastService.error('Failed to load pets data');
    } finally {
      this.isLoading = false;
    }
  }

  private async loadPetImages() {
    const petIdsAndExtensions = this.allPets()
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
    // With server-side pagination, we keep the static species options
    // or we could load species from a separate API endpoint
    const uniqueSpecies = [
      ...new Set(this.allPets().map((pet) => pet.species)),
    ].filter(Boolean);

    // Add any new species found in current page to existing options
    const existingSpecies = this.speciesOptions
      .filter((option) => option.value !== null)
      .map((option) => option.value);

    const newSpecies = uniqueSpecies.filter(
      (species) => species && !existingSpecies.includes(species),
    );

    if (newSpecies.length > 0) {
      this.speciesOptions = [
        { label: 'All Species', value: null },
        ...this.speciesOptions.slice(1), // Keep existing non-null options
        ...newSpecies.map((species) => ({ label: species!, value: species! })),
      ];
    }
  }

  // NOTE: Filters are now applied server-side via API parameters.
  // This provides better performance and accurate pagination across all results.

  onSearchChange() {
    // Clear existing timer
    if (this.searchDebounceTimer) {
      clearTimeout(this.searchDebounceTimer);
    }

    // Set a new timer to debounce the search
    this.searchDebounceTimer = setTimeout(() => {
      // Reset to first page when filters change
      this.resetPagination();
      this.loadPetsData(0);
    }, 500); // 500ms debounce
  }

  onStatusChange() {
    // Reset to first page when filters change
    this.resetPagination();
    this.loadPetsData(0);
  }

  onSpeciesChange() {
    // Reset to first page when filters change
    this.resetPagination();
    this.loadPetsData(0);
  }

  clearFilters() {
    this.searchName = '';
    this.searchBreed = '';
    this.selectedStatus = null;
    this.selectedSpecies = null;
    this.resetPagination();
    this.loadPetsData(0);
  }

  onPageChange(event: PaginatorState) {
    const page = event.page || 0;
    const rows = event.rows || this.pageSize;

    // If page size changed, reset pagination and update page size
    if (rows !== this.pageSize) {
      this.pageSize = rows;
      this.resetPagination();
      this.loadPetsData(0);
    } else {
      this.loadPetsData(page);
    }
  }

  private resetPagination() {
    this.currentPage = 0;
    this.pageTokens.clear();
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
        this.resetPagination();
        this.loadPetsData(0); // Refresh the data
      }
    });
  }

  onEditPet(pet: Pet) {
    // TODO: Implement edit pet functionality
    console.log('Edit pet:', pet.name);
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
          this.loadPetsData(this.currentPage); // Refresh the current page
        } catch (error) {
          console.error('Error deleting pet:', error);
          this.toastService.error('Failed to delete pet');
        }
      },
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
          this.toastService.success(
            `${pet.name}'s status updated to ${newStatus}`,
          );
          this.loadPetsData(this.currentPage); // Refresh the current page
        } catch (error) {
          console.error('Error updating pet status:', error);
          this.toastService.error('Failed to update pet status');
        }
      },
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

  getAgeLabel(dateOfBirth: string | undefined): string {
    return getAgeLabel(dateOfBirth);
  }
}
