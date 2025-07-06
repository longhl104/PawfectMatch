import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Pet } from '../../pages/home/home.component';

@Component({
  selector: 'app-pet-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pet-card.component.html',
  styleUrls: ['./pet-card.component.scss']
})
export class PetCardComponent {
  @Input() pet!: Pet;
  @Output() statusChange = new EventEmitter<{petId: string, newStatus: Pet['status']}>();

  onStatusChange(newStatus: Pet['status']): void {
    this.statusChange.emit({ petId: this.pet.id, newStatus });
  }

  onStatusSelectChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.onStatusChange(target.value as Pet['status']);
  }

  getStatusColor(status: Pet['status']): string {
    switch (status) {
      case 'available': return '#48bb78';
      case 'pending': return '#ed8936';
      case 'adopted': return '#4299e1';
      default: return '#718096';
    }
  }

  getStatusIcon(status: Pet['status']): string {
    switch (status) {
      case 'available': return '✓';
      case 'pending': return '⏳';
      case 'adopted': return '🏠';
      default: return '?';
    }
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    }).format(date);
  }
}
