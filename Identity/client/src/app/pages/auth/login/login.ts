import { Component, inject } from '@angular/core';

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
import { environment } from 'environments/environment';

interface HttpError {
  status?: number;
  error?: {
    message?: string;
  };
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterModule],
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
      };

      try {
        const response = await firstValueFrom(
          this.usersService.login(loginRequest),
        );

        this.toastService.success(`Welcome back!`);

        switch (response.data?.user?.userType) {
          case 'adopter':
            window.location.href = environment.matcherUrl;
            break;
          case 'shelter_admin':
            window.location.href = environment.shelterHubUrl;
            break;
          default:
            throw new Error(
              `Unknown user type: ${response.data?.user?.userType}`,
            );
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
