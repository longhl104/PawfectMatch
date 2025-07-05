import { Component, inject } from '@angular/core';
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
import { firstValueFrom } from 'rxjs';
import {
  ToastService,
  ErrorHandlingService,
} from '@longhl104/pawfect-match-ng';
import {
  ShelterAdminRegistrationRequest,
  ShelterAdminService,
  ShelterAdminRegistrationResponse,
} from '../../../../shared/services/shelter-admin.service';
import { AddressInputComponent, AddressDetails } from '../../../../shared/components/address-input/address-input.component';

@Component({
  selector: 'app-shelter-admin-registration',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, AddressInputComponent],
  templateUrl: './registration.html',
  styleUrl: './registration.scss',
})
export class ShelterAdminRegistration {
  private formBuilder = inject(FormBuilder);
  private router = inject(Router);
  private shelterAdminService = inject(ShelterAdminService);
  private toastService = inject(ToastService);
  private errorHandlingService = inject(ErrorHandlingService);

  registrationForm: FormGroup;
  showPassword = false;
  showConfirmPassword = false;
  isSubmitting = false;
  selectedAddress: AddressDetails | null = null;

  constructor() {
    this.registrationForm = this.createForm();
  }

  private createForm(): FormGroup {
    const form = this.formBuilder.group(
      {
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
        shelterName: ['', [Validators.required, Validators.minLength(2)]],
        shelterContactNumber: ['', [Validators.required, this.australianPhoneValidator]],
        shelterAddress: ['', [Validators.required]],
        shelterWebsiteUrl: ['', [this.urlValidator]],
        shelterAbn: ['', [this.abnValidator]],
        shelterDescription: ['', [Validators.maxLength(1000)]],
        agreeToTerms: [false, [Validators.requiredTrue]],
      },
      {
        validators: this.passwordMatchValidator,
      },
    );

    // Add custom validator to confirmPassword field
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
    control: AbstractControl,
  ): ValidationErrors | null {
    const value = control.value;
    if (!value) return null;

    const hasUpperCase = /[A-Z]/.test(value);
    const hasLowerCase = /[a-z]/.test(value);
    const hasNumeric = /[0-9]/.test(value);
    const hasSpecialChar = /[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/.test(value);

    const passwordValid =
      hasUpperCase && hasLowerCase && hasNumeric && hasSpecialChar;

    return passwordValid ? null : { pattern: true };
  }

  // Custom validator for Australian phone numbers
  private australianPhoneValidator(
    control: AbstractControl,
  ): ValidationErrors | null {
    if (!control.value) return null;

    const phoneRegex = /^(\+61|0)[2-9][0-9]{8}$/;
    return phoneRegex.test(control.value) ? null : { pattern: true };
  }

  // Custom validator for URL
  private urlValidator(
    control: AbstractControl,
  ): ValidationErrors | null {
    if (!control.value) return null;

    try {
      new URL(control.value);
      return null;
    } catch {
      return { invalidUrl: true };
    }
  }

  // Custom validator for ABN
  private abnValidator(
    control: AbstractControl,
  ): ValidationErrors | null {
    if (!control.value) return null;

    // Basic ABN validation (11 digits)
    const abnRegex = /^\d{11}$/;
    return abnRegex.test(control.value.replace(/\s/g, '')) ? null : { pattern: true };
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
    form: AbstractControl,
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

  toggleConfirmPassword(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.registrationForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  async onSubmit(): Promise<void> {
    // Add additional validation for address
    const addressControl = this.registrationForm.get('shelterAddress');
    if (addressControl?.value && !this.selectedAddress) {
      addressControl.setErrors({ invalidAddress: true });
    }

    if (this.registrationForm.valid && this.selectedAddress) {
      this.isSubmitting = true;

      const formData = this.registrationForm.value;
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      const { confirmPassword, agreeToTerms, ...registrationData } = formData;
      // confirmPassword and agreeToTerms are excluded from registration data

      const finalData: ShelterAdminRegistrationRequest = {
        ...registrationData,
        shelterAddress: this.selectedAddress.formattedAddress,
        shelterAddressDetails: this.selectedAddress,
      };

      try {
        console.log('Shelter admin registration data:', finalData);

        const response = await firstValueFrom(
          this.shelterAdminService.register(finalData),
        ) as ShelterAdminRegistrationResponse;

        console.log('Registration successful');
        this.toastService.success(
          'Registration successful! Welcome to PawfectMatch.',
        );

        if (response.redirectUrl) {
          window.location.href = response.redirectUrl;
        }
      } catch (error) {
        this.errorHandlingService.handleErrorWithComponent(
          error,
          this,
          'shelter-admin-registration',
        );
      } finally {
        this.isSubmitting = false;
      }
    } else {
      // Mark all fields as touched to show validation errors
      Object.keys(this.registrationForm.controls).forEach((key) => {
        this.registrationForm.get(key)?.markAsTouched();
      });

      this.toastService.error('Please fill in all required fields correctly.');
    }
  }

  goBack(): void {
    this.router.navigate(['/auth/choice']);
  }

  onAddressSelected(addressDetails: AddressDetails | null): void {
    this.selectedAddress = addressDetails;
  }
}
