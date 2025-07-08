import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToolbarModule } from 'primeng/toolbar';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import {
  AuthService,
  type AuthStatusResponse,
  type UserProfile,
} from 'shared/services/auth.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    ToolbarModule,
    ButtonModule,
    AvatarModule,
    MenuModule,
  ],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent implements OnInit {
  private authService = inject(AuthService);

  authStatus$: Observable<AuthStatusResponse>;
  userMenuItems: MenuItem[] = [];

  constructor() {
    this.authStatus$ = this.authService.authStatus$;
  }

  ngOnInit() {
    this.setupUserMenu();
  }

  onLogin() {
    this.authService.redirectToLogin();
  }

  onLogout() {
    this.authService.logout().subscribe({
      next: (response) => {
        console.log('Logout successful:', response);
      },
      error: (error) => {
        console.error('Logout failed:', error);
      },
    });
  }

  private setupUserMenu() {
    this.userMenuItems = [
      {
        label: 'Profile',
        icon: 'pi pi-user',
        command: () => {
          // Navigate to profile page
          console.log('Navigate to profile');
        },
      },
      {
        label: 'Settings',
        icon: 'pi pi-cog',
        command: () => {
          // Navigate to settings page
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
    return user.fullName || user.email || 'User';
  }

  getUserInitials(user: UserProfile | undefined): string {
    const name = this.getUserDisplayName(user);
    return name
      .split(' ')
      .map((n) => n[0])
      .join('')
      .toUpperCase()
      .substring(0, 2);
  }
}
