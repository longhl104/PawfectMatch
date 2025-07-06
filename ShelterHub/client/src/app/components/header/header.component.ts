import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService, UserProfile } from 'shared/services/auth.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss'],
})
export class HeaderComponent {
  private authService = inject(AuthService);

  isMenuOpen = false;
  currentUser$: Observable<UserProfile | null> = this.authService.currentUser$;
  isAuthenticated$ = this.authService.authStatus$;

  toggleMenu() {
    this.isMenuOpen = !this.isMenuOpen;
  }

  closeMenu() {
    this.isMenuOpen = false;
  }

  onLogin() {
    this.authService.redirectToLogin();
  }

  onLogout() {
    this.authService.logout().subscribe();
  }

  onAddPet() {
    console.log('Navigating to add pet form...');
    // TODO: Navigate to add pet form
  }

  onRunMatching() {
    console.log('Running matching algorithm...');
    // TODO: Trigger matching algorithm
  }
}
