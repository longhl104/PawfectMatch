import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DataViewModule } from 'primeng/dataview';
import { PanelModule } from 'primeng/panel';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { ShelterService, type ShelterInfo } from '../../shared/services/shelter.service';
import { PetService, type Pet } from '../../shared/services/pet.service';
import { ApplicationService, type Application } from '../../shared/services/application.service';
import { AddPetFormComponent } from './add-pet-form/add-pet-form.component';

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
    ToastModule,
    AddPetFormComponent
  ],
  providers: [MessageService],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private shelterService = inject(ShelterService);
  private petService = inject(PetService);
  private applicationService = inject(ApplicationService);
  private messageService = inject(MessageService);

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
      this.recentApplications = await this.applicationService.getRecentApplications();

    } catch (error) {
      console.error('Error loading dashboard data:', error);
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: 'Failed to load dashboard data'
      });
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

  getApplicationStatusSeverity(status: string): 'success' | 'info' | 'warning' | 'danger' {
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
      this.messageService.add({
        severity: 'info',
        summary: 'Matching Started',
        detail: 'Running pet matching algorithm...'
      });

      await this.applicationService.runMatching();

      this.messageService.add({
        severity: 'success',
        summary: 'Matching Complete',
        detail: 'Pet matching has been completed successfully'
      });

      // Reload applications to show updated match scores
      this.recentApplications = await this.applicationService.getRecentApplications();

    } catch (error) {
      console.error('Error running matcher:', error);
      this.messageService.add({
        severity: 'error',
        summary: 'Matching Failed',
        detail: 'Failed to run pet matching algorithm'
      });
    } finally {
      this.isRunningMatcher = false;
    }
  }

  onPetAdded() {
    this.showAddPetDialog = false;
    this.loadDashboardData(); // Refresh the data
    this.messageService.add({
      severity: 'success',
      summary: 'Pet Added',
      detail: 'New pet has been added successfully'
    });
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString();
  }
}
