import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService, UserProfile } from 'shared/services/auth.service';
import { Observable } from 'rxjs';
import { ToolbarModule } from 'primeng/toolbar';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';
import { MenuModule } from 'primeng/menu';
import { TooltipModule } from 'primeng/tooltip';
import { MenuItem } from 'primeng/api';
import { CustomIconComponent } from '@longhl104/pawfect-match-ng';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ToolbarModule,
    ButtonModule,
    AvatarModule,
    MenuModule,
    TooltipModule,
    CustomIconComponent,
  ],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss'],
})
export class HeaderComponent {
  private authService = inject(AuthService);

  isMenuOpen = false;
  isMenuClosing = false;
  currentUser$: Observable<UserProfile | null> = this.authService.currentUser$;
  isAuthenticated$ = this.authService.authStatus$;
  userMenuItems: MenuItem[] = [];

  constructor() {
    this.setupUserMenu();
  }

  toggleMenu() {
    this.isMenuOpen = !this.isMenuOpen;
    this.isMenuClosing = false;
  }

  closeMenu() {
    if (this.isMenuOpen) {
      this.isMenuClosing = true;
      // Wait for animation to complete before hiding
      setTimeout(() => {
        this.isMenuOpen = false;
        this.isMenuClosing = false;
      }, 300); // Match animation duration
    }
  }

  onLogin() {
    this.authService.redirectToLogin();
  }

  onSignUp() {
    this.authService.redirectToSignUp();
  }

  onLogout() {
    this.authService.logout().subscribe();
  }

  private setupUserMenu() {
    this.userMenuItems = [
      {
        label: 'Profile',
        icon: 'pi pi-user',
        command: () => {
          console.log('Navigate to profile');
        },
      },
      {
        label: 'Settings',
        icon: 'pi pi-cog',
        command: () => {
          console.log('Navigate to settings');
        },
      },
      {
        separator: true,
      },
      {
        label: 'Logout',
        icon: 'pi pi-sign-out',
        command: () => {
          this.onLogout();
        },
      },
    ];
  }

  getUserDisplayName(user: UserProfile | undefined): string {
    if (!user) return 'User';
    const fullName = `${user.firstName || ''} ${user.lastName || ''}`.trim();
    return fullName || user.email || 'User';
  }

  getUserInitials(user: UserProfile | undefined): string {
    if (!user) return 'U';
    const firstName = user.firstName || '';
    const lastName = user.lastName || '';
    return (firstName.charAt(0) + lastName.charAt(0)).toUpperCase() || 'U';
  }
}
