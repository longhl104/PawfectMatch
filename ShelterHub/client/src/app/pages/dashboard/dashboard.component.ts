import { filter, firstValueFrom } from 'rxjs';
import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DataViewModule } from 'primeng/dataview';
import { PanelModule } from 'primeng/panel';
import { TableModule } from 'primeng/table';
import { ShelterService } from 'shared/services/shelter.service';
import {
  ApplicationService,
  type Application,
} from 'shared/services/application.service';
import { ToastService } from '@longhl104/pawfect-match-ng';
import { AuthService } from 'shared/services/auth.service';
import { PetService } from 'shared/services/pet.service';
import {
  GetPetImageDownloadUrlsRequest,
  Pet,
  PetImageDownloadUrlRequest,
  PetsApi,
  Shelter,
  ShelterPetStatisticsResponse,
} from 'shared/apis/generated-apis';
import { PetCardComponent } from 'shared/components';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    CardModule,
    ButtonModule,
    TagModule,
    DataViewModule,
    PanelModule,
    TableModule,
    PetCardComponent,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private shelterService = inject(ShelterService);
  private petService = inject(PetService);
  private applicationService = inject(ApplicationService);
  private toastService = inject(ToastService);
  private petsApi = inject(PetsApi);
  private router = inject(Router);
  public authService = inject(AuthService);

  shelterInfo: Shelter | null = null;
  petStatistics: ShelterPetStatisticsResponse | null = null;
  pets: Pet[] = [];
  recentApplications: Application[] = [];
  isLoading = true;
  isRunningMatcher = false;
  petMainImageUrls = new Map<string, string>();

  ngOnInit() {
    this.shelterService.shelter$
      .pipe(filter((shelter) => !!shelter))
      .subscribe((shelter) => {
        this.loadDashboardData(shelter);
      });
  }

  async loadDashboardData(shelter: Shelter) {
    try {
      this.isLoading = true;

      // Load shelter information
      this.shelterInfo = shelter;

      // Load pet statistics
      try {
        this.petStatistics = await this.shelterService.getPetStatistics();
      } catch (error) {
        console.error('Failed to load pet statistics:', error);
        this.toastService.error('Failed to load pet statistics');
        this.petStatistics = null;
      }

      // Load pets
      this.pets = await this.petService.getAllPets(this.shelterInfo.shelterId);

      const petIdsAndExtensions = this.pets
        .map((pet) => ({
          petId: pet.petPostgreSqlId!,
          mainImageFileExtension: pet.mainImageFileExtension,
        }))
        .filter((pet) => pet.mainImageFileExtension);

      if (petIdsAndExtensions.length > 0) {
        // Fetch main image URLs for all pets
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
        } else {
          this.toastService.error(
            'Failed to load pet images: ' + petImagesResponse.errorMessage,
          );
        }
      }

      // Load recent applications
      this.recentApplications =
        await this.applicationService.getRecentApplications();
    } finally {
      this.isLoading = false;
    }
  }

  getApplicationStatusSeverity(
    status: string,
  ): 'success' | 'info' | 'warning' | 'danger' {
    switch (status) {
      case 'approved':
        return 'success';
      case 'pending':
        return 'info';
      case 'rejected':
        return 'danger';
      default:
        return 'info';
    }
  }

  onAddPet() {
    if (!this.shelterInfo) {
      this.toastService.error('Shelter information is not available');
      return;
    }

    // Navigate to edit-pet page in add mode
    this.router.navigate(['/pets/edit'], {
      queryParams: { mode: 'add', shelterId: this.shelterInfo.shelterId }
    });
  }

  async onRunMatching() {
    try {
      this.isRunningMatcher = true;
      this.toastService.info('Matching Started');

      await this.applicationService.runMatching();

      this.toastService.success('Matching Complete');

      // Reload applications to show updated match scores
      this.recentApplications =
        await this.applicationService.getRecentApplications();
    } finally {
      this.isRunningMatcher = false;
    }
  }

  onPetAdded() {
    if (!this.shelterInfo) {
      throw new Error('Shelter information is not available');
    }

    this.loadDashboardData(this.shelterInfo); // Refresh the data including pet statistics
  }

  onSeeFullList() {
    this.router.navigate(['/pets']);
  }

  onEditPet(pet: Pet) {
    this.router.navigate(['/pets/edit', pet.petId]);
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString();
  }

  getAdoptedPetsCount(): number {
    if (!this.petStatistics) {
      return 0;
    }
    // Note: AdoptedPets property actually contains the count of adopted pets
    return this.petStatistics.adoptedPets || 0;
  }

  onImageError(event: Event) {
    // Hide the broken image and show the fallback
    const target = event.target as HTMLImageElement;
    target.style.display = 'none';

    // Show the fallback icon container
    const parent = target.parentElement;
    if (parent) {
      parent.innerHTML =
        '<div class="w-full h-full bg-gray-100 flex align-items-center justify-content-center"><i class="pi pi-image text-4xl text-gray-400"></i></div>';
    }
  }
}
