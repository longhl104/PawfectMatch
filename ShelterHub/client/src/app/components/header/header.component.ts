import { Component, inject, OnDestroy, Renderer2, HostListener } from '@angular/core';
import { CommonModule, DOCUMENT } from '@angular/common';
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
export class HeaderComponent implements OnDestroy {
  private authService = inject(AuthService);
  private document = inject(DOCUMENT);
  private renderer = inject(Renderer2);

  isMenuOpen = false;
  isManagementDropdownOpen = false;
  isReportsDropdownOpen = false;
  currentUser$: Observable<UserProfile | null> = this.authService.currentUser$;
  isAuthenticated$ = this.authService.authStatus$;

  ngOnDestroy() {
    // Ensure body scroll is restored when component is destroyed
    if (this.isMenuOpen) {
      this.renderer.removeClass(this.document.body, 'menu-open');
    }
  }

  toggleMenu() {
    this.isMenuOpen = !this.isMenuOpen;

    if (this.isMenuOpen) {
      this.renderer.addClass(this.document.body, 'menu-open');
    } else {
      this.renderer.removeClass(this.document.body, 'menu-open');
    }
  }

  closeMenu() {
    if (this.isMenuOpen) {
      this.isMenuOpen = false;
      this.renderer.removeClass(this.document.body, 'menu-open');
    }
    this.closeAllDropdowns();
  }

  toggleManagementDropdown() {
    this.isManagementDropdownOpen = !this.isManagementDropdownOpen;
    this.isReportsDropdownOpen = false;
  }

  toggleReportsDropdown() {
    this.isReportsDropdownOpen = !this.isReportsDropdownOpen;
    this.isManagementDropdownOpen = false;
  }

  closeAllDropdowns() {
    this.isManagementDropdownOpen = false;
    this.isReportsDropdownOpen = false;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event) {
    // Close dropdowns when clicking outside
    const target = event.target as HTMLElement;
    if (!target.closest('.dropdown')) {
      this.closeAllDropdowns();
    }
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
