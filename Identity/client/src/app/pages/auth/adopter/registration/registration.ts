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
        state: ['', [Validators.required]], // Add state field
        location: [{ value: '', disabled: true }, [Validators.required]],
        postcode: [
          { value: '', disabled: true },
          [
            Validators.required,
            Validators.pattern(/^\d{4}$/),
            this.postcodeStateValidator(),
          ],
        ],
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

    // Enable/disable location and postcode fields based on state selection
    form.get('state')?.valueChanges.subscribe((stateValue) => {
      const locationControl = form.get('location');
      const postcodeControl = form.get('postcode');

      if (stateValue) {
        locationControl?.enable();
        postcodeControl?.enable();
        this.updateLocationPlaceholder(stateValue);
        this.updatePostcodePlaceholder(stateValue);
      } else {
        locationControl?.disable();
        postcodeControl?.disable();
        locationControl?.setValue('');
        postcodeControl?.setValue('');
      }
    });

    // Validate postcode when state or postcode changes
    form.get('postcode')?.valueChanges.subscribe(() => {
      form.get('postcode')?.updateValueAndValidity({ emitEvent: false });
    });

    return form;
  }

  // Custom validator for postcode based on state
  private postcodeStateValidator() {
    return (control: AbstractControl): ValidationErrors | null => {
      const postcode = control.value;
      const state = control.parent?.get('state')?.value;

      if (!postcode || !state) return null;

      // Australian postcode ranges by state
      const postcodeRanges: { [key: string]: (code: number) => boolean } = {
        NSW: (code) =>
          (code >= 1000 && code <= 1999) || (code >= 2000 && code <= 2999),
        ACT: (code) =>
          (code >= 200 && code <= 299) ||
          (code >= 2600 && code <= 2699) ||
          (code >= 2900 && code <= 2920),
        VIC: (code) =>
          (code >= 3000 && code <= 3999) || (code >= 8000 && code <= 8999),
        QLD: (code) =>
          (code >= 4000 && code <= 4999) || (code >= 9000 && code <= 9999),
        SA: (code) => code >= 5000 && code <= 5999,
        WA: (code) =>
          (code >= 6000 && code <= 6797) || (code >= 6800 && code <= 6999),
        TAS: (code) => code >= 7000 && code <= 7999,
        NT: (code) => code >= 800 && code <= 999,
      };

      const postcodeNum = parseInt(postcode, 10);
      const isValidRange = postcodeRanges[state]?.(postcodeNum);

      return isValidRange ? null : { invalidStatePostcode: true };
    };
  }

  // Update postcode placeholder based on selected state
  private updatePostcodePlaceholder(state: string): void {
    const postcodeInput = document.getElementById(
      'postcode'
    ) as HTMLInputElement;
    if (postcodeInput) {
      const placeholders: { [key: string]: string } = {
        NSW: 'e.g., 2000, 2300, 2500',
        VIC: 'e.g., 3000, 3220, 3350',
        QLD: 'e.g., 4000, 4217, 4870',
        WA: 'e.g., 6000, 6160, 6230',
        SA: 'e.g., 5000, 5290, 5600',
        TAS: 'e.g., 7000, 7250, 6450',
        ACT: 'e.g., 2600, 2900, 2614',
        NT: 'e.g., 0800, 0870, 0850',
      };

      postcodeInput.placeholder = placeholders[state] || 'Enter postcode';
    }
  }

  // Update location placeholder based on selected state
  private updateLocationPlaceholder(state: string): void {
    const locationInput = document.getElementById(
      'location'
    ) as HTMLInputElement;
    if (locationInput) {
      const placeholders: { [key: string]: string } = {
        NSW: 'e.g., Sydney, Newcastle, Wollongong',
        VIC: 'e.g., Melbourne, Geelong, Ballarat',
        QLD: 'e.g., Brisbane, Gold Coast, Cairns',
        WA: 'e.g., Perth, Fremantle, Bunbury',
        SA: 'e.g., Adelaide, Mount Gambier, Whyalla',
        TAS: 'e.g., Hobart, Launceston, Burnie',
        ACT: 'e.g., Canberra, Tuggeranong, Belconnen',
        NT: 'e.g., Darwin, Alice Springs, Katherine',
      };

      locationInput.placeholder =
        placeholders[state] || 'Enter your city or suburb';
    }
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

        // Combine location and postcode for full address identification
        const fullLocation = `${registrationData.location}, ${registrationData.state} ${registrationData.postcode}`;

        console.log('Registration data:', {
          ...registrationData,
          fullLocation,
        });

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
