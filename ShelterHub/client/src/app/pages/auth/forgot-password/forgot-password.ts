import { Component, inject } from '@angular/core';

import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.scss',
})
export class ForgotPassword {
  private router = inject(Router);

  goBack(): void {
    this.router.navigate(['/auth/login']);
  }
}
