import { Routes } from '@angular/router';

export const adopterRoutes: Routes = [
  {
    path: '',
    redirectTo: 'register',
    pathMatch: 'full',
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./registration/registration').then((m) => m.Registration),
    title: 'Adopter Registration',
  },
];
