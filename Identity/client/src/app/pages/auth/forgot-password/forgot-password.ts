import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup, AbstractControl } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { FloatLabelModule } from 'primeng/floatlabel';
import { MessageModule } from 'primeng/message';
import { AuthApi, ForgotPasswordRequest, ResetPasswordRequest } from '../../../shared/apis/generated-apis';
import { firstValueFrom } from 'rxjs';

interface StepConfig {
  title: string;
  description: string;
  icon: string;
}

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    InputTextModule,
    PasswordModule,
    ButtonModule,
    CardModule,
    FloatLabelModule,
    MessageModule
  ],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.scss',
})
export class ForgotPassword {
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private authApi = inject(AuthApi);

  // Form states
  emailForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  resetForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    resetCode: ['', [Validators.required, Validators.minLength(6)]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]]
  }, { validators: this.passwordMatchValidator });

  // Component state
  currentStep = signal<'email' | 'reset'>('email');
  isLoading = signal(false);
  userEmail = signal('');
  errorMessage = signal('');
  successMessage = signal('');

  // Step configurations
  stepConfigs: Record<'email' | 'reset', StepConfig> = {
    email: {
      title: 'Reset Your Password',
      description: 'Enter your email address and we\'ll send you a verification code',
      icon: 'pi-envelope'
    },
    reset: {
      title: 'Enter Verification Code',
      description: 'We\'ve sent a verification code to your email address',
      icon: 'pi-key'
    }
  };

  get currentStepConfig() {
    return this.stepConfigs[this.currentStep()];
  }

  // Password match validator
  passwordMatchValidator(form: AbstractControl) {
    const password = form.get('newPassword');
    const confirmPassword = form.get('confirmPassword');

    if (password && confirmPassword && password.value !== confirmPassword.value) {
      confirmPassword.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }

    if (confirmPassword?.hasError('passwordMismatch')) {
      confirmPassword.setErrors(null);
    }

    return null;
  }

  async requestPasswordReset() {
    if (this.emailForm.invalid) {
      this.markFormGroupTouched(this.emailForm);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    try {
      const email = this.emailForm.value.email!;
      const request = new ForgotPasswordRequest({ email });

      const response = await firstValueFrom(this.authApi.forgotPassword(request));

      if (response.success) {
        this.userEmail.set(email);
        this.resetForm.patchValue({ email });
        this.currentStep.set('reset');
        this.successMessage.set(response.message || 'Verification code sent successfully');
      } else {
        this.errorMessage.set(response.message || 'Failed to send verification code');
      }
    } catch (error: unknown) {
      console.error('Password reset request failed:', error);
      this.errorMessage.set('Failed to send verification code. Please try again.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async resetPassword() {
    if (this.resetForm.invalid) {
      this.markFormGroupTouched(this.resetForm);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    try {
      const formValue = this.resetForm.value;
      const request = new ResetPasswordRequest({
        email: formValue.email!,
        resetCode: formValue.resetCode!,
        newPassword: formValue.newPassword!
      });

      const response = await firstValueFrom(this.authApi.resetPassword(request));

      if (response.success) {
        this.successMessage.set(response.message || 'Password reset successfully');
        // Redirect to login after successful reset
        setTimeout(() => {
          this.router.navigate(['/auth/login'], {
            queryParams: { message: 'Password reset successfully. Please log in with your new password.' }
          });
        }, 2000);
      } else {
        this.errorMessage.set(response.message || 'Failed to reset password');
      }
    } catch (error: unknown) {
      console.error('Password reset failed:', error);
      this.errorMessage.set('Failed to reset password. Please check your verification code and try again.');
    } finally {
      this.isLoading.set(false);
    }
  }

  resendCode() {
    // Reset to email step to resend code
    this.currentStep.set('email');
    this.emailForm.patchValue({ email: this.userEmail() });
    this.errorMessage.set('');
    this.successMessage.set('');
  }

  goBack(): void {
    if (this.currentStep() === 'reset') {
      this.currentStep.set('email');
    } else {
      this.router.navigate(['/auth/login']);
    }
  }

  // Helper method to get form control error message
  getFieldError(formGroup: FormGroup, fieldName: string): string {
    const field = formGroup.get(fieldName);
    if (field?.errors && field.touched) {
      if (field.errors['required']) return `${this.getFieldLabel(fieldName)} is required`;
      if (field.errors['email']) return 'Please enter a valid email address';
      if (field.errors['minlength']) return `${this.getFieldLabel(fieldName)} must be at least ${field.errors['minlength'].requiredLength} characters`;
      if (field.errors['passwordMismatch']) return 'Passwords do not match';
    }
    return '';
  }

  // Helper method to check if field is invalid
  isFieldInvalid(formGroup: FormGroup, fieldName: string): boolean {
    const field = formGroup.get(fieldName);
    return !!(field?.invalid && field?.touched);
  }

  private getFieldLabel(fieldName: string): string {
    const labels: Record<string, string> = {
      email: 'Email',
      resetCode: 'Verification code',
      newPassword: 'New password',
      confirmPassword: 'Confirm password'
    };
    return labels[fieldName] || fieldName;
  }

  private markFormGroupTouched(formGroup: FormGroup) {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }
}
