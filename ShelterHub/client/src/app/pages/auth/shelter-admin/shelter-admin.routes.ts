import { Routes } from '@angular/router';

export const shelterAdminRoutes: Routes = [
  {
    path: '',
    redirectTo: 'register',
    pathMatch: 'full',
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./registration/registration').then((m) => m.ShelterAdminRegistration),
    title: 'Shelter Admin Registration',
  },
];
