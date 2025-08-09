import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { TooltipModule } from 'primeng/tooltip';
import { Pet } from '../types/pet.interface';

@Component({
  selector: 'app-contact-shelter-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ButtonModule,
    DialogModule,
    TooltipModule,
  ],
  templateUrl: './contact-shelter-dialog.component.html',
  styleUrls: ['./contact-shelter-dialog.component.scss']
})
export class ContactShelterDialogComponent {
  @Input() visible = signal(false);
  @Input() pet = signal<Pet | null>(null);
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() viewPetDetails = new EventEmitter<Pet>();

  onHide() {
    this.visible.set(false);
    this.visibleChange.emit(false);
  }

  callShelter(phoneNumber: string) {
    window.open(`tel:${phoneNumber}`);
  }

  visitWebsite(website: string) {
    window.open(website, '_blank');
  }

  sendEmail(email: string) {
    window.open(`mailto:${email}`);
  }

  onViewPetDetails() {
    if (this.pet()) {
      this.viewPetDetails.emit(this.pet()!);
      this.onHide();
    }
  }
}
