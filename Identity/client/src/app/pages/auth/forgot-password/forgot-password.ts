import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.scss',
})
export class ForgotPassword {
  private router = inject(Router);

  goBack(): void {
    this.router.navigate(['/auth/login']);
  }
}
