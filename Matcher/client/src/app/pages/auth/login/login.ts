import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { LoginRequest, UsersService } from 'shared/services/users.service';
import { firstValueFrom } from 'rxjs';
import {
  ToastService,
  ErrorHandlingService,
} from '@longhl104/pawfect-match-ng';

interface HttpError {
  status?: number;
  error?: {
    message?: string;
  };
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private formBuilder = inject(FormBuilder);
  private router = inject(Router);
  private usersService = inject(UsersService);
  private toastService = inject(ToastService);
  private errorHandlingService = inject(ErrorHandlingService);

  loginForm: FormGroup;
  showPassword = false;
  isSubmitting = false;

  constructor() {
    this.loginForm = this.createForm();
  }

  private createForm(): FormGroup {
    return this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]],
      rememberMe: [false],
    });
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.loginForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  async onSubmit(): Promise<void> {
    if (this.loginForm.valid) {
      this.isSubmitting = true;

      const formData = this.loginForm.value;
      const loginRequest: LoginRequest = {
        email: formData.email,
        password: formData.password,
        userType: formData.userType,
      };

      try {
        const response = await firstValueFrom(
          this.usersService.login(loginRequest),
        );

        // Store tokens (you might want to use a more secure storage method)
        if (formData.rememberMe) {
          localStorage.setItem('accessToken', response.accessToken);
          localStorage.setItem('refreshToken', response.refreshToken);
        } else {
          sessionStorage.setItem('accessToken', response.accessToken);
          sessionStorage.setItem('refreshToken', response.refreshToken);
        }

        // Store user info
        localStorage.setItem('currentUser', JSON.stringify(response.user));

        this.toastService.success(`Welcome back, ${response.user.fullName}!`);

        // Navigate based on user type
        if (response.user.userType === 'adopter') {
          this.router.navigate(['/adopter/dashboard']);
        } else {
          this.router.navigate(['/shelter/dashboard']);
        }
      } catch (error: unknown) {
        // Handle specific error cases
        const httpError = error as HttpError;
        if (httpError.status === 401) {
          this.toastService.error(
            'Invalid email or password. Please try again.',
          );
        } else if (httpError.error?.message?.includes('not verified')) {
          this.toastService.error('Please verify your email address first.');
          // Offer to resend verification code
          this.handleUnverifiedUser(formData.email, formData.userType);
        } else {
          this.errorHandlingService.handleErrorWithComponent(
            error,
            this,
            'login',
          );
        }
      } finally {
        this.isSubmitting = false;
      }
    } else {
      // Mark all fields as touched to show validation errors
      Object.keys(this.loginForm.controls).forEach((key) => {
        this.loginForm.get(key)?.markAsTouched();
      });
      this.toastService.error('Please fill in all required fields correctly.');
    }
  }

  private async handleUnverifiedUser(email: string, userType: string) {
    // Navigate to code verification with option to resend
    this.router.navigate(['/auth/code-verification'], {
      queryParams: { email, userType },
    });
  }

  goBack(): void {
    this.router.navigate(['/auth/choice']);
  }
}
