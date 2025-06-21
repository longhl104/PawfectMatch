import { Routes } from '@angular/router';
import { authRoutes } from './pages/auth/auth.routes';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/auth',
    pathMatch: 'full',
  },
  {
    path: 'auth',
    children: authRoutes,
  },
];
