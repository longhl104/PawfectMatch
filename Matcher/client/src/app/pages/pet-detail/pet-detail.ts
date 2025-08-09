import { ChangeDetectionStrategy, Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { DividerModule } from 'primeng/divider';
import { TooltipModule } from 'primeng/tooltip';
import { Pet } from '../browse/types/pet.interface';
import { ContactShelterDialogComponent } from '../browse/contact-shelter-dialog/contact-shelter-dialog.component';

@Component({
  selector: 'app-pet-detail',
  imports: [
    CommonModule,
    ButtonModule,
    CardModule,
    TagModule,
    DividerModule,
    TooltipModule,
    ContactShelterDialogComponent,
  ],
  templateUrl: './pet-detail.html',
  styleUrl: './pet-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PetDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  pet = signal<Pet | null>(null);
  isLoading = signal(false);
  showContactDialog = signal(false);

  ngOnInit() {
    // Get pet ID from route parameter
    const petId = this.route.snapshot.paramMap.get('petId');

    // Try to get pet data from navigation state first
    const navigation = this.router.getCurrentNavigation();
    const petData = navigation?.extras?.state?.['pet'] as Pet;

    if (petData) {
      // We have pet data from navigation state
      this.pet.set(petData);
    } else {
      // Try to get from sessionStorage as backup
      const storedPet = sessionStorage.getItem('selectedPet');
      if (storedPet) {
        try {
          const parsedPet = JSON.parse(storedPet) as Pet;
          // Verify this is the correct pet by checking the ID
          if (parsedPet.petPostgreSqlId.toString() === petId) {
            this.pet.set(parsedPet);
            return;
          }
        } catch (error) {
          console.error('Error parsing stored pet data:', error);
        }
      }

      // If we get here, we don't have valid pet data
      if (petId) {
        // In a real app, you'd fetch this from an API using the pet ID
        console.log('Pet ID from route:', petId);
        console.log('No pet data available, would fetch from API');
        // For now, redirect back to browse since we don't have the pet data
        this.router.navigate(['/browse']);
      } else {
        // No pet ID, redirect back to browse
        this.router.navigate(['/browse']);
      }
    }
  }  onContactShelter() {
    this.showContactDialog.set(true);
  }

  onApplyForAdoption() {
    const currentPet = this.pet();
    if (currentPet) {
      // Navigate to adoption application form with pet data
      this.router.navigate(['/adoption-application'], {
        state: { pet: currentPet }
      });
    }
  }

  onGoBack() {
    this.router.navigate(['/browse']);
  }

  onFavorite() {
    const currentPet = this.pet();
    if (currentPet) {
      // TODO: Implement favorite functionality
      console.log('Add to favorites:', currentPet);
    }
  }

  onShare() {
    const currentPet = this.pet();
    if (currentPet && navigator.share) {
      navigator.share({
        title: `Meet ${currentPet.name}`,
        text: `Check out this adorable ${currentPet.species} looking for a home!`,
        url: window.location.href,
      }).catch(console.error);
    } else if (currentPet) {
      // Fallback: copy to clipboard
      navigator.clipboard.writeText(window.location.href).then(() => {
        console.log('URL copied to clipboard');
      });
    }
  }
}
