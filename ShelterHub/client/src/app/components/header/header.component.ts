import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService, UserProfile } from 'shared/services/auth.service';
import { Observable } from 'rxjs';

// PrimeNG imports
import { ButtonModule } from 'primeng/button';
import { MenubarModule } from 'primeng/menubar';
import { MenuModule } from 'primeng/menu';
import { AvatarModule } from 'primeng/avatar';
import { BadgeModule } from 'primeng/badge';
import { TooltipModule } from 'primeng/tooltip';
import { MenuItem } from 'primeng/api';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ButtonModule,
    MenubarModule,
    MenuModule,
    AvatarModule,
    BadgeModule,
    TooltipModule
  ],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss'],
})
export class HeaderComponent {
  private authService = inject(AuthService);

  currentUser$: Observable<UserProfile | null> = this.authService.currentUser$;
  isAuthenticated$ = this.authService.authStatus$;

  menuItems: MenuItem[] = [
    {
      label: 'Dashboard',
      icon: 'pi pi-home',
      routerLink: '/home'
    },
    {
      label: 'Management',
      icon: 'pi pi-cog',
      items: [
        {
          label: 'Manage Pets',
          icon: 'pi pi-heart',
          routerLink: '/pets'
        },
        {
          label: 'Applications',
          icon: 'pi pi-file',
          routerLink: '/applications'
        },
        {
          label: 'Adoptions',
          icon: 'pi pi-check',
          routerLink: '/adoptions'
        }
      ]
    },
    {
      label: 'Reports',
      icon: 'pi pi-chart-bar',
      items: [
        {
          label: 'Analytics',
          icon: 'pi pi-chart-line',
          routerLink: '/reports'
        },
        {
          label: 'Adoption Reports',
          icon: 'pi pi-chart-pie',
          routerLink: '/reports/adoptions'
        },
        {
          label: 'Matching Analytics',
          icon: 'pi pi-search',
          routerLink: '/reports/matching'
        }
      ]
    },
    {
      label: 'Settings',
      icon: 'pi pi-wrench',
      routerLink: '/settings'
    }
  ];

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
