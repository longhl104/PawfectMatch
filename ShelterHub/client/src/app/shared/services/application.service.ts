import { Injectable } from '@angular/core';

export interface Application {
  id: string;
  petId: string;
  petName: string;
  applicantName: string;
  applicantEmail: string;
  status: 'pending' | 'approved' | 'rejected';
  submittedDate: Date;
  matchScore?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ApplicationService {

  async getRecentApplications(): Promise<Application[]> {
    // TODO: Replace with actual API call
    return Promise.resolve([
      {
        id: '1',
        petId: '1',
        petName: 'Buddy',
        applicantName: 'John Smith',
        applicantEmail: 'john.smith@email.com',
        status: 'pending',
        submittedDate: new Date('2024-12-15'),
        matchScore: 85
      },
      {
        id: '2',
        petId: '2',
        petName: 'Whiskers',
        applicantName: 'Sarah Johnson',
        applicantEmail: 'sarah.johnson@email.com',
        status: 'approved',
        submittedDate: new Date('2024-12-10'),
        matchScore: 92
      },
      {
        id: '3',
        petId: '4',
        petName: 'Luna',
        applicantName: 'Mike Davis',
        applicantEmail: 'mike.davis@email.com',
        status: 'pending',
        submittedDate: new Date('2024-12-12'),
        matchScore: 78
      },
      {
        id: '4',
        petId: '1',
        petName: 'Buddy',
        applicantName: 'Emily Wilson',
        applicantEmail: 'emily.wilson@email.com',
        status: 'rejected',
        submittedDate: new Date('2024-12-08'),
        matchScore: 45
      }
    ]);
  }

  async getApplicationById(id: string): Promise<Application | null> {
    const applications = await this.getRecentApplications();
    return applications.find(app => app.id === id) || null;
  }

  async updateApplicationStatus(applicationId: string, status: Application['status']): Promise<Application> {
    // TODO: Replace with actual API call
    const application = await this.getApplicationById(applicationId);
    if (!application) {
      throw new Error('Application not found');
    }
    application.status = status;
    console.log('Updating application status:', application);
    return Promise.resolve(application);
  }

  async runMatching(): Promise<void> {
    // TODO: Replace with actual API call to matching service
    console.log('Running pet matching algorithm...');
    // Simulate API delay
    await new Promise(resolve => setTimeout(resolve, 2000));
    console.log('Matching completed');
  }

  async getApplicationsForPet(petId: string): Promise<Application[]> {
    const applications = await this.getRecentApplications();
    return applications.filter(app => app.petId === petId);
  }
}
