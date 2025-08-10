import { Component, inject } from '@angular/core';

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
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { IftaLabelModule } from 'primeng/iftalabel';
import { CheckboxModule } from 'primeng/checkbox';
import { TextareaModule } from 'primeng/textarea';
import {
  ToastService,
  ErrorHandlingService,
} from '@longhl104/pawfect-match-ng';
import { AddressInputComponent, AddressDetails } from 'shared/components';
import {
  AdopterRegistrationRequest,
  AdoptersService,
} from 'shared/services/adopters.service';

@Component({
  selector: 'app-registration',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterModule,
    AddressInputComponent,
    InputTextModule,
    PasswordModule,
    ButtonModule,
    CardModule,
    IftaLabelModule,
    CheckboxModule,
    TextareaModule,
  ],
  templateUrl: './registration.html',
  styleUrl: './registration.scss',
})
export class Registration {
  private formBuilder = inject(FormBuilder);
  private router = inject(Router);
  private adoptersService = inject(AdoptersService);
  private toastService = inject(ToastService);
  private errorHandlingService = inject(ErrorHandlingService);

  registrationForm: FormGroup;
  isSubmitting = false;
  selectedAddress: AddressDetails | null = null;

  constructor() {
    this.registrationForm = this.createForm();
  }

  onAddressSelected(addressDetails: AddressDetails | null): void {
    this.selectedAddress = addressDetails;
  }

  private createForm(): FormGroup {
    const form = this.formBuilder.group(
      {
        firstName: ['', [Validators.required, Validators.minLength(2)]],
        lastName: ['', [Validators.required, Validators.minLength(2)]],
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
        address: ['', [Validators.required]],
        bio: [''],
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
    form: AbstractControl,
  ): ValidationErrors | null {
    const password = form.get('password');
    const confirmPassword = form.get('confirmPassword');

    if (!password || !confirmPassword) return null;

    return password.value === confirmPassword.value
      ? null
      : { passwordMismatch: true };
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.registrationForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  async onSubmit(): Promise<void> {
    // Add additional validation for address
    const addressControl = this.registrationForm.get('address');
    if (addressControl?.value && !this.selectedAddress) {
      addressControl.setErrors({ invalidAddress: true });
    }

    if (this.registrationForm.valid && this.selectedAddress) {
      this.isSubmitting = true;

      const formData = this.registrationForm.value;
      const { firstName, lastName, ...restData } = formData;

      // Include the detailed address information
      const finalData: AdopterRegistrationRequest = {
        ...restData,
        fullName: `${firstName} ${lastName}`.trim(),
        address: this.selectedAddress.formattedAddress,
        addressDetails: this.selectedAddress,
      };

      try {
        console.log('Registration data:', finalData);

        const response = await firstValueFrom(
          this.adoptersService.register(finalData),
        );

        console.log('Registration successful');
        this.toastService.success(
          'Registration successful! Please check your email for a verification code.',
        );

        if (response.redirectUrl) {
          window.location.href = response.redirectUrl;
        }
      } catch (error) {
        this.errorHandlingService.handleErrorWithComponent(
          error,
          this,
          'registration',
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
}
