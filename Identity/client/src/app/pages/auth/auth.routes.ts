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
    title: 'Choose Registration Type',
  },
  {
    path: 'adopter',
    loadChildren: () =>
      import('./adopter/adopter.routes').then((m) => m.adopterRoutes),
  },
  {
    path: 'code-verification',
    loadComponent: () =>
      import('./code-verification/code-verification').then(
        (m) => m.CodeVerification
      ),
    title: 'Code Verification',
  },
];
