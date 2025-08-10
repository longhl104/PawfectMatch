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
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { IftaLabelModule } from 'primeng/iftalabel';
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
  imports: [
    ReactiveFormsModule,
    RouterModule,
    InputTextModule,
    PasswordModule,
    ButtonModule,
    CardModule,
    IftaLabelModule
  ],
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

        console.log('=== Login Response Debug ===');
        console.log('Response:', response);
        console.log('Response data:', response.data);
        console.log('User type:', response.data?.user?.userType);

        this.toastService.success(`Welcome back!`);

        // Wait for cookies to be set, then navigate
        await this.waitForCookiesAndNavigate(
          response.data?.user?.userType || '',
        );
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

  private async waitForCookiesAndNavigate(userType: string): Promise<void> {
    console.log('=== Cookie Debug Information ===');
    console.log('Current URL:', window.location.href);
    console.log('Current domain:', window.location.hostname);
    console.log('Current protocol:', window.location.protocol);
    console.log('Current port:', window.location.port);
    console.log('Document domain:', document.domain);
    console.log('All cookies before wait:', document.cookie);
    console.log('Cookie length:', document.cookie.length);

    // Check if we're in a secure context
    console.log('Is secure context:', window.isSecureContext);

    // Wait for cookies to be set (check multiple times with delays)
    const maxAttempts = 10;
    let attempts = 0;

    while (attempts < maxAttempts) {
      // Check if cookies are available
      const accessToken = this.getCookie('accessToken');
      const userInfo = this.getCookie('userInfo');

      console.log(`=== Attempt ${attempts + 1} ===`);
      console.log('Raw document.cookie:', document.cookie);
      console.log('Cookie exists check - accessToken:', !!accessToken);
      console.log('Cookie exists check - userInfo:', !!userInfo);
      console.log(
        'Cookie value check - accessToken length:',
        accessToken?.length || 0,
      );
      console.log(
        'Cookie value check - userInfo length:',
        userInfo?.length || 0,
      );

      // Try to parse all cookies manually
      const allCookies = this.getAllCookies();
      console.log('Parsed cookies:', allCookies);

      if (accessToken && userInfo) {
        console.log('✅ Cookies detected, navigating...');
        this.navigateToCorrectApp(userType);
        return;
      }

      // Wait 100ms before checking again
      await new Promise((resolve) => setTimeout(resolve, 100));
      attempts++;
    }

    // If cookies still not available after 1 second, navigate anyway
    console.warn(
      '❌ Cookies not detected after 1 second, navigating anyway...',
    );
    console.log('Final cookie state:', document.cookie);
    console.log('Final cookie length:', document.cookie.length);
    console.log('=== End Cookie Debug ===');
    this.navigateToCorrectApp(userType);
  }

  private getCookie(name: string): string | null {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) {
      return parts.pop()?.split(';').shift() || null;
    }
    return null;
  }

  private getAllCookies(): Record<string, string> {
    const cookies: Record<string, string> = {};
    if (document.cookie) {
      document.cookie.split(';').forEach((cookie) => {
        const [name, ...rest] = cookie.trim().split('=');
        if (name) {
          cookies[name] = rest.join('=');
        }
      });
    }
    return cookies;
  }

  private navigateToCorrectApp(userType: string): void {
    const currentHost = window.location.hostname;
    let targetUrl: string;

    switch (userType) {
      case 'adopter':
        targetUrl = environment.matcherUrl;
        break;
      case 'shelter_admin':
        targetUrl = environment.shelterHubUrl;
        break;
      default:
        throw new Error(`Unknown user type: ${userType}`);
    }

    console.log(`Navigating from ${currentHost} to ${targetUrl}`);
    window.location.href = targetUrl;
  }
}
