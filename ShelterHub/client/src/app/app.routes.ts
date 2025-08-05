import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full',
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./pages/dashboard/dashboard.component').then(
        (m) => m.DashboardComponent,
      ),
  },
  {
    path: 'pets',
    loadComponent: () =>
      import('./pages/pets/pets-list.component').then(
        (m) => m.PetsListComponent,
      ),
  },
  {
    path: 'pets/edit/:id',
    loadComponent: () =>
      import('./pages/pets/edit-pet/edit-pet.component').then(
        (m) => m.EditPetComponent,
      ),
  },
  {
    path: 'pets/edit',
    loadComponent: () =>
      import('./pages/pets/edit-pet/edit-pet.component').then(
        (m) => m.EditPetComponent,
      ),
  },
  {
    path: '**',
    redirectTo: '/dashboard',
    pathMatch: 'full',
  },
];
