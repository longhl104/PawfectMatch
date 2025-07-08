import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DataViewModule } from 'primeng/dataview';
import { PanelModule } from 'primeng/panel';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import {
  ShelterService,
  type ShelterInfo,
} from '../../shared/services/shelter.service';
import { PetService, type Pet } from '../../shared/services/pet.service';
import {
  ApplicationService,
  type Application,
} from '../../shared/services/application.service';
import { AddPetFormComponent } from './add-pet-form/add-pet-form.component';
import { ToastService } from '@longhl104/pawfect-match-ng';

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
    DialogModule,
    AddPetFormComponent,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private shelterService = inject(ShelterService);
  private petService = inject(PetService);
  private applicationService = inject(ApplicationService);
  private toastService = inject(ToastService);

  shelterInfo: ShelterInfo | null = null;
  pets: Pet[] = [];
  recentApplications: Application[] = [];
  isLoading = true;
  showAddPetDialog = false;
  isRunningMatcher = false;

  ngOnInit() {
    this.loadDashboardData();
  }

  async loadDashboardData() {
    try {
      this.isLoading = true;

      // Load shelter information
      this.shelterInfo = await this.shelterService.getShelterInfo();

      // Load pets
      this.pets = await this.petService.getAllPets();

      // Load recent applications
      this.recentApplications =
        await this.applicationService.getRecentApplications();
    } finally {
      this.isLoading = false;
    }
  }

  getStatusSeverity(status: string): 'success' | 'info' | 'warning' | 'danger' {
    switch (status) {
      case 'available':
        return 'success';
      case 'pending':
        return 'info';
      case 'adopted':
        return 'warning';
      case 'medical_hold':
        return 'danger';
      default:
        return 'info';
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
    this.showAddPetDialog = true;
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
    this.showAddPetDialog = false;
    this.loadDashboardData(); // Refresh the data
    this.toastService.success('New pet has been added successfully');
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString();
  }
}
