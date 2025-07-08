import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService, UserProfile } from 'shared/services/auth.service';
import { Observable } from 'rxjs';

// PrimeNG imports
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { AvatarModule } from 'primeng/avatar';
import { BadgeModule } from 'primeng/badge';
import { DataViewModule } from 'primeng/dataview';
import { PanelModule } from 'primeng/panel';
import { ChipModule } from 'primeng/chip';
import { SkeletonModule } from 'primeng/skeleton';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ButtonModule,
    CardModule,
    TableModule,
    TagModule,
    AvatarModule,
    BadgeModule,
    DataViewModule,
    PanelModule,
    ChipModule,
    SkeletonModule
  ],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
})
export class HomeComponent {
  private authService = inject(AuthService);

  currentUser$: Observable<UserProfile | null> = this.authService.currentUser$;
  isAuthenticated$ = this.authService.authStatus$;

  // Shelter pets with status
  shelterPets = [
    {
      id: 1,
      name: 'Buddy',
      breed: 'Golden Retriever',
      age: 3,
      status: 'Available',
      applications: 5,
      image: 'https://images.unsplash.com/photo-1552053831-71594a27632d?w=400&h=300&fit=crop',
      description: 'Friendly and energetic, loves playing fetch!',
      dateAdded: '2024-12-15',
      medicalStatus: 'Up to date',
    },
    {
      id: 2,
      name: 'Luna',
      breed: 'Border Collie',
      age: 2,
      status: 'Pending Adoption',
      applications: 12,
      image: 'https://images.unsplash.com/photo-1551717743-49959800b1f6?w=400&h=300&fit=crop',
      description: 'Intelligent and loyal, great with kids!',
      dateAdded: '2024-12-10',
      medicalStatus: 'Needs checkup',
    },
    {
      id: 3,
      name: 'Max',
      breed: 'Labrador Mix',
      age: 4,
      status: 'Available',
      applications: 8,
      image: 'https://images.unsplash.com/photo-1543466835-00a7907e9de1?w=400&h=300&fit=crop',
      description: 'Gentle giant who loves cuddles and walks!',
      dateAdded: '2024-12-08',
      medicalStatus: 'Up to date',
    },
    {
      id: 4,
      name: 'Bella',
      breed: 'Beagle',
      age: 1,
      status: 'Medical Hold',
      applications: 3,
      image: 'https://images.unsplash.com/photo-1544717297-fa95b6ee9643?w=400&h=300&fit=crop',
      description: 'Playful puppy looking for an active family!',
      dateAdded: '2024-12-20',
      medicalStatus: 'In treatment',
    },
    {
      id: 5,
      name: 'Charlie',
      breed: 'German Shepherd',
      age: 5,
      status: 'Available',
      applications: 15,
      image: 'https://images.unsplash.com/photo-1589941013453-ec89f33b5e95?w=400&h=300&fit=crop',
      description: 'Loyal and protective, great guard dog!',
      dateAdded: '2024-12-05',
      medicalStatus: 'Up to date',
    },
    {
      id: 6,
      name: 'Daisy',
      breed: 'Poodle Mix',
      age: 2,
      status: 'Adopted',
      applications: 7,
      image: 'https://images.unsplash.com/photo-1583337130417-3346a1be7dee?w=400&h=300&fit=crop',
      description: 'Hypoallergenic and friendly, loves everyone!',
      dateAdded: '2024-11-28',
      medicalStatus: 'Up to date',
    },
  ];

  // Recent applications
  recentApplications = [
    {
      id: 1,
      applicantName: 'Sarah Johnson',
      petName: 'Luna',
      submittedDate: '2024-12-22',
      status: 'Under Review',
      contactInfo: 'sarah.johnson@email.com',
    },
    {
      id: 2,
      applicantName: 'Mike Chen',
      petName: 'Charlie',
      submittedDate: '2024-12-21',
      status: 'Approved',
      contactInfo: 'mike.chen@email.com',
    },
    {
      id: 3,
      applicantName: 'Lisa Rodriguez',
      petName: 'Buddy',
      submittedDate: '2024-12-20',
      status: 'Interview Scheduled',
      contactInfo: 'lisa.rodriguez@email.com',
    },
    {
      id: 4,
      applicantName: 'David Wilson',
      petName: 'Max',
      submittedDate: '2024-12-19',
      status: 'Under Review',
      contactInfo: 'david.wilson@email.com',
    },
    {
      id: 5,
      applicantName: 'Emma Thompson',
      petName: 'Bella',
      submittedDate: '2024-12-18',
      status: 'Pending Information',
      contactInfo: 'emma.thompson@email.com',
    },
  ];

  // Dashboard statistics
  dashboardStats = {
    totalPets: 24,
    availablePets: 18,
    pendingAdoptions: 4,
    newApplications: 8,
    adoptedThisMonth: 12,
  };

  // Admin action handlers
  onAddPet() {
    console.log('Navigating to add pet form...');
    // TODO: Navigate to add pet form
  }

  onRunMatching() {
    console.log('Running matching algorithm...');
    // TODO: Trigger matching algorithm
  }

  onViewPet(petId: number) {
    console.log('Viewing pet details:', petId);
    // TODO: Navigate to pet management page
  }

  onViewApplication(applicationId: number) {
    console.log('Viewing application:', applicationId);
    // TODO: Navigate to application details
  }

  onManagePet(petId: number) {
    console.log('Managing pet:', petId);
    // TODO: Navigate to pet management
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'available':
        return 'status-available';
      case 'pending adoption':
        return 'status-pending';
      case 'adopted':
        return 'status-adopted';
      case 'medical hold':
        return 'status-medical';
      case 'under review':
        return 'status-review';
      case 'approved':
        return 'status-approved';
      case 'interview scheduled':
        return 'status-interview';
      case 'pending information':
        return 'status-pending-info';
      default:
        return 'status-default';
    }
  }

  getStatusSeverity(status: string): "success" | "info" | "warn" | "danger" | "secondary" | "contrast" | undefined {
    switch (status.toLowerCase()) {
      case 'available':
        return 'success';
      case 'pending adoption':
        return 'warn';
      case 'adopted':
        return 'info';
      case 'medical hold':
        return 'danger';
      case 'under review':
        return 'secondary';
      case 'approved':
        return 'success';
      case 'interview scheduled':
        return 'info';
      case 'pending information':
        return 'warn';
      default:
        return 'secondary';
    }
  }
}
