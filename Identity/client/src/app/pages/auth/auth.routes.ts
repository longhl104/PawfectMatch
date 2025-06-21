import { Routes } from '@angular/router';
import { Choice } from './choice/choice';

export const authRoutes: Routes = [
  {
    path: '',
    redirectTo: 'choice',
    pathMatch: 'full',
  },
  {
    path: 'choice',
    component: Choice,
  },
];
