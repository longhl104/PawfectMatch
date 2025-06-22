import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-registration',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './registration.html',
  styleUrl: './registration.scss',
})
export class Registration {
  registrationForm: FormGroup;
  showPassword = false;
  isSubmitting = false;

  constructor(private formBuilder: FormBuilder, private router: Router) {
    this.registrationForm = this.createForm();
  }

  private createForm(): FormGroup {
    const form = this.formBuilder.group(
      {
        fullName: ['', [Validators.required, Validators.minLength(2)]],
        email: ['', [Validators.required, Validators.email]],
        password: [
          '',
          [
            Validators.required,
            Validators.minLength(8),
            this.strongPasswordValidator,
          ],
        ],
        confirmPassword: ['', [Validators.required]],
        phoneNumber: ['', [this.australianPhoneValidator]],
        location: ['', [Validators.required]],
        bio: [''],
        agreeToTerms: [false, [Validators.requiredTrue]],
      },
      {
        validators: this.passwordMatchValidator,
      }
    );

    // Add custom validator to confirmPassword field and set up cross-field validation
    const confirmPasswordControl = form.get('confirmPassword');
    confirmPasswordControl?.setValidators([
      Validators.required,
      this.confirmPasswordValidator(form),
    ]);

    // Update confirmPassword validation when password changes
    form.get('password')?.valueChanges.subscribe(() => {
      confirmPasswordControl?.updateValueAndValidity({ emitEvent: false });
    });

    return form;
  }

  // Custom validator for strong password
  private strongPasswordValidator(
    control: AbstractControl
  ): ValidationErrors | null {
    const value = control.value;
    if (!value) return null;

    const hasUpperCase = /[A-Z]/.test(value);
    const hasLowerCase = /[a-z]/.test(value);
    const hasNumeric = /[0-9]/.test(value);
    const hasSpecialChar = /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(value);

    const passwordValid =
      hasUpperCase && hasLowerCase && hasNumeric && hasSpecialChar;

    return passwordValid ? null : { pattern: true };
  }

  // Custom validator for Australian phone numbers
  private australianPhoneValidator(
    control: AbstractControl
  ): ValidationErrors | null {
    const value = control.value;
    if (!value) return null; // Optional field

    // Australian phone number patterns
    const mobilePattern = /^04\d{8}$/; // Mobile: 04XXXXXXXX
    const landlinePattern = /^0[2-8]\d{8}$/; // Landline: 0XXXXXXXXX
    const formattedPattern = /^04\d{2}\s\d{3}\s\d{3}$/; // Formatted: 04XX XXX XXX

    const cleanedValue = value.replace(/\s/g, '');
    const isValid =
      mobilePattern.test(cleanedValue) ||
      landlinePattern.test(cleanedValue) ||
      formattedPattern.test(value);

    return isValid ? null : { pattern: true };
  }

  // Custom validator for confirm password field
  private confirmPasswordValidator(form: FormGroup) {
    return (control: AbstractControl): ValidationErrors | null => {
      const password = form.get('password');
      if (!password || !control.value) return null;

      return password.value === control.value
        ? null
        : { passwordMismatch: true };
    };
  }

  // Custom validator to check if passwords match
  private passwordMatchValidator(
    form: AbstractControl
  ): ValidationErrors | null {
    const password = form.get('password');
    const confirmPassword = form.get('confirmPassword');

    if (!password || !confirmPassword) return null;

    return password.value === confirmPassword.value
      ? null
      : { passwordMismatch: true };
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.registrationForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  async onSubmit(): Promise<void> {
    if (this.registrationForm.valid) {
      this.isSubmitting = true;

      try {
        const formData = this.registrationForm.value;

        // Remove confirmPassword from the data to be sent
        const { confirmPassword, ...registrationData } = formData;

        console.log('Registration data:', registrationData);

        // TODO: Call registration service
        // await this.authService.registerAdopter(registrationData);

        // Simulate API call
        await new Promise((resolve) => setTimeout(resolve, 2000));

        // Navigate to success page or login
        this.router.navigate(['/auth/login'], {
          queryParams: { message: 'Registration successful! Please log in.' },
        });
      } catch (error) {
        console.error('Registration failed:', error);
        // Handle error (show toast, etc.)
      } finally {
        this.isSubmitting = false;
      }
    } else {
      // Mark all fields as touched to show validation errors
      Object.keys(this.registrationForm.controls).forEach((key) => {
        this.registrationForm.get(key)?.markAsTouched();
      });
    }
  }
}
