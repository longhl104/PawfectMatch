import { Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/landing/landing.component').then(
        (m) => m.LandingComponent,
      ),
  },
  {
    path: 'browse',
    loadComponent: () =>
      import('./pages/browse/browse').then((m) => m.BrowseComponent),
  },
  {
    path: 'pet-detail/:petId',
    loadComponent: () =>
      import('./pages/pet-detail/pet-detail').then((m) => m.PetDetailComponent),
  },
  {
    path: 'adoption-application',
    loadComponent: () =>
      import('./pages/adoption-application/adoption-application').then((m) => m.AdoptionApplicationComponent),
  },
  {
    path: 'about',
    loadComponent: () =>
      import('./pages/about/about.component').then((m) => m.AboutComponent),
  },
  {
    path: 'contact',
    loadComponent: () =>
      import('./pages/contact/contact').then((m) => m.ContactComponent),
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./pages/dashboard/dashboard.component').then(
        (m) => m.DashboardComponent,
      ),
    canActivate: [AuthGuard],
  },
  {
    path: '**',
    redirectTo: '',
    pathMatch: 'full',
  },
];
