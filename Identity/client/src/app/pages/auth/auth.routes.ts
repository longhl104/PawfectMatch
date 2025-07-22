import { Routes } from '@angular/router';

export const authRoutes: Routes = [
  {
    path: '',
    redirectTo: 'choice',
    pathMatch: 'full',
  },
  {
    path: 'choice',
    loadComponent: () => import('./choice/choice').then((m) => m.Choice),
    title: 'Choose Registration Type',
  },
  {
    path: 'adopter',
    loadChildren: () =>
      import('./adopter/adopter.routes').then((m) => m.adopterRoutes),
  },
  {
    path: 'shelter-admin',
    loadChildren: () =>
      import('./shelter-admin/shelter-admin.routes').then(
        (m) => m.shelterAdminRoutes,
      ),
  },
  {
    path: 'login',
    loadComponent: () => import('./login/login').then((m) => m.Login),
    title: 'Sign In',
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./forgot-password/forgot-password').then((m) => m.ForgotPassword),
  },
];
