import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { Pet, PetStatus } from '../../apis/generated-apis';
import { getAgeLabel as getAgeLabelFromUtils } from '../../utils';

export interface PetCardAction {
  type: 'edit' | 'delete' | 'status-change';
  icon: string;
  severity?: 'success' | 'info' | 'warn' | 'danger' | 'secondary';
  tooltip?: string;
  outlined?: boolean;
  size?: 'small' | 'large';
  data?: PetStatus; // For status changes, this would contain the new status
}

@Component({
  selector: 'app-pet-card',
  standalone: true,
  imports: [CommonModule, CardModule, TagModule, ButtonModule, TooltipModule],
  templateUrl: './pet-card.component.html',
  styleUrl: './pet-card.component.scss',
})
export class PetCardComponent {
  readonly pet = input.required<Pet>();
  readonly imageUrl = input<string>();
  readonly showActions = input(false);
  readonly showStatusActions = input(false);
  readonly showWeight = input(false);
  readonly showDescription = input(false);
  readonly truncateDescription = input(true);
  readonly descriptionMaxLength = input(100);

  readonly imageError = output<Event>();
  readonly actionClick = output<{
    action: PetCardAction;
    pet: Pet;
    event: Event;
}>();

  getAgeLabel(dateOfBirth: string | undefined): string {
    return getAgeLabelFromUtils(dateOfBirth);
  }

  getStatusSeverity(
    status: string | undefined,
  ): 'success' | 'info' | 'warning' | 'danger' {
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
    this.imageError.emit(event);
  }

  onActionClick(action: PetCardAction, event: Event) {
    this.actionClick.emit({ action, pet: this.pet(), event });
  }

  // Status change actions
  getAvailableAction(): PetCardAction {
    return {
      type: 'status-change',
      icon: 'pi pi-check',
      severity: 'success',
      tooltip: 'Mark as Available',
      outlined: true,
      size: 'small',
      data: PetStatus.Available,
    };
  }

  getPendingAction(): PetCardAction {
    return {
      type: 'status-change',
      icon: 'pi pi-clock',
      severity: 'info',
      tooltip: 'Mark as Pending',
      outlined: true,
      size: 'small',
      data: PetStatus.Pending,
    };
  }

  getAdoptedAction(): PetCardAction {
    return {
      type: 'status-change',
      icon: 'pi pi-heart',
      severity: 'secondary',
      tooltip: 'Mark as Adopted',
      outlined: true,
      size: 'small',
      data: PetStatus.Adopted,
    };
  }

  // Edit/Delete actions
  getEditAction(): PetCardAction {
    return {
      type: 'edit',
      icon: 'pi pi-pencil',
      tooltip: 'Edit Pet',
      outlined: true,
      size: 'small',
    };
  }

  getDeleteAction(): PetCardAction {
    return {
      type: 'delete',
      icon: 'pi pi-trash',
      severity: 'danger',
      tooltip: 'Delete Pet',
      outlined: true,
      size: 'small',
    };
  }

  // Check if status actions should be shown
  shouldShowAvailableAction(): boolean {
    return this.pet().status !== PetStatus.Available;
  }

  shouldShowPendingAction(): boolean {
    return this.pet().status !== PetStatus.Pending;
  }

  shouldShowAdoptedAction(): boolean {
    return this.pet().status !== PetStatus.Adopted;
  }

  // Description handling
  getDisplayDescription(): string {
    const pet = this.pet();
    if (!pet.description) return '';

    if (
      this.truncateDescription() &&
      pet.description.length > this.descriptionMaxLength()
    ) {
      return pet.description.substring(0, this.descriptionMaxLength()) + '...';
    }

    return pet.description;
  }
}
