import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Application } from '../../pages/home/home.component';

@Component({
  selector: 'app-application-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './application-section.component.html',
  styleUrls: ['./application-section.component.scss']
})
export class ApplicationSectionComponent {
  @Input() applications: Application[] = [];
  @Output() statusChange = new EventEmitter<{applicationId: string, newStatus: Application['status']}>();

  onStatusChange(applicationId: string, newStatus: Application['status']): void {
    this.statusChange.emit({ applicationId, newStatus });
  }

  onStatusSelectChange(event: Event, applicationId: string): void {
    const target = event.target as HTMLSelectElement;
    this.onStatusChange(applicationId, target.value as Application['status']);
  }

  getStatusColor(status: Application['status']): string {
    switch (status) {
      case 'pending': return '#ed8936';
      case 'approved': return '#48bb78';
      case 'rejected': return '#f56565';
      default: return '#718096';
    }
  }

  getStatusIcon(status: Application['status']): string {
    switch (status) {
      case 'pending': return '⏳';
      case 'approved': return '✅';
      case 'rejected': return '❌';
      default: return '?';
    }
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(date);
  }

  trackByApplicationId(index: number, application: Application): string {
    return application.id;
  }
}
