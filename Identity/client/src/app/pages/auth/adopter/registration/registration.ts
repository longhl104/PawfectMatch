import {
  AdopterRegistrationRequest,
  AdoptersService,
} from 'shared/services/adopters.service';
import {
  Component,
  NgZone,
  ViewChild,
  ElementRef,
  afterNextRender,
} from '@angular/core';
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
import { GoogleMapsService } from 'shared/services/google-maps.service';
import { ToastService } from 'shared/services/toast.service';
import { firstValueFrom } from 'rxjs';

declare const google: any;

@Component({
  selector: 'app-registration',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './registration.html',
  styleUrl: './registration.scss',
})
export class Registration {
  @ViewChild('addressInput', { static: false }) addressInputRef!: ElementRef;

  registrationForm: FormGroup;
  showPassword = false;
  isSubmitting = false;
  isGoogleMapsLoading = true;

  private autocomplete: any;
  selectedAddress: any = null;

  constructor(
    private formBuilder: FormBuilder,
    private router: Router,
    private ngZone: NgZone,
    private googleMapsService: GoogleMapsService,
    private adoptersService: AdoptersService,
    private toastService: ToastService
  ) {
    this.registrationForm = this.createForm();

    afterNextRender(async () => {
      try {
        await this.googleMapsService.loadGoogleMaps();
        this.isGoogleMapsLoading = false;

        // Enable the address field when Google Maps is loaded
        this.registrationForm.get('address')?.enable();

        // Wait for view to be ready
        setTimeout(() => this.initializeAutocomplete(), 100);
      } catch (error) {
        console.error('Failed to load Google Maps:', error);
        this.isGoogleMapsLoading = false;

        // Still enable the field even if Google Maps fails to load
        this.registrationForm.get('address')?.enable();

        // Show fallback or error message
        this.toastService.warning(
          'Google Maps failed to load. Please type your address manually.'
        );
      }
    });
  }

  private initializeAutocomplete() {
    if (!this.addressInputRef?.nativeElement) {
      setTimeout(() => this.initializeAutocomplete(), 100);
      return;
    }

    const options = {
      componentRestrictions: { country: 'AU' },
      fields: ['formatted_address', 'address_components', 'geometry'],
      types: ['geocode'],
    };

    this.autocomplete = new google.maps.places.Autocomplete(
      this.addressInputRef.nativeElement,
      options
    );

    this.autocomplete.addListener('place_changed', () => {
      this.ngZone.run(() => {
        const place = this.autocomplete.getPlace();

        if (!place.address_components) {
          this.registrationForm
            .get('address')
            ?.setErrors({ invalidAddress: true });
          this.selectedAddress = null;
          return;
        }

        this.selectedAddress = this.extractAddressComponents(place);
        this.registrationForm.get('address')?.setValue(place.formatted_address);
        this.registrationForm.get('address')?.setErrors(null);
      });
    });

    // Add input event listener to validate manual typing
    this.addressInputRef.nativeElement.addEventListener(
      'input',
      (event: any) => {
        this.ngZone.run(() => {
          const currentValue = event.target.value;
          const addressControl = this.registrationForm.get('address');

          // If user is typing and the current value doesn't match our selected address,
          // mark as invalid
          if (
            currentValue &&
            (!this.selectedAddress ||
              this.selectedAddress.formattedAddress !== currentValue)
          ) {
            addressControl?.setErrors({ invalidAddress: true });
          }

          // If field is empty, only show required error
          if (!currentValue) {
            this.selectedAddress = null;
            addressControl?.setErrors({ required: true });
          }
        });
      }
    );

    // Add blur event to validate when user leaves the field
    this.addressInputRef.nativeElement.addEventListener('blur', () => {
      this.ngZone.run(() => {
        const addressControl = this.registrationForm.get('address');
        const currentValue = addressControl?.value;

        if (
          currentValue &&
          (!this.selectedAddress ||
            this.selectedAddress.formattedAddress !== currentValue)
        ) {
          addressControl?.setErrors({ invalidAddress: true });
        }
      });
    });
  }

  private extractAddressComponents(place: any) {
    const components = place.address_components;
    const result = {
      streetNumber: '',
      streetName: '',
      suburb: '',
      city: '',
      state: '',
      postcode: '',
      country: '',
      formattedAddress: place.formatted_address,
      latitude: place.geometry?.location?.lat(),
      longitude: place.geometry?.location?.lng(),
    };

    components.forEach((component: any) => {
      const types = component.types;

      if (types.includes('street_number')) {
        result.streetNumber = component.long_name;
      }
      if (types.includes('route')) {
        result.streetName = component.long_name;
      }
      if (types.includes('locality')) {
        result.suburb = component.long_name;
      }
      if (types.includes('administrative_area_level_2')) {
        result.city = component.long_name;
      }
      if (types.includes('administrative_area_level_1')) {
        result.state = component.short_name; // NSW, VIC, etc.
      }
      if (types.includes('postal_code')) {
        result.postcode = component.long_name;
      }
      if (types.includes('country')) {
        result.country = component.short_name;
      }
    });

    return result;
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
        address: [
          { value: '', disabled: this.isGoogleMapsLoading },
          [Validators.required],
        ], // Initialize with disabled state
        bio: [''],
        agreeToTerms: [false, [Validators.requiredTrue]],
      },
      {
        validators: this.passwordMatchValidator,
      }
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
    // Add additional validation for address
    const addressControl = this.registrationForm.get('address');
    if (addressControl?.value && !this.selectedAddress) {
      addressControl.setErrors({ invalidAddress: true });
    }

    if (this.registrationForm.valid && this.selectedAddress) {
      this.isSubmitting = true;

      try {
        const formData = this.registrationForm.value;
        const { confirmPassword, ...registrationData } = formData;

        // Include the detailed address information
        const finalData: AdopterRegistrationRequest = registrationData;

        console.log('Registration data:', finalData);

        await firstValueFrom(this.adoptersService.register(finalData));
        console.log('Registration successful');
        this.toastService.success(
          'Registration successful! Welcome to PawfectMatch!'
        );

        this.router.navigate(['/auth/login']);
      } catch (error) {
        console.error('Registration failed:', error);
        this.toastService.error('Registration failed. Please try again.');
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
