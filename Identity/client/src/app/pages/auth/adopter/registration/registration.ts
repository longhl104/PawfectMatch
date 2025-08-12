import { Component, inject, OnInit } from '@angular/core';

import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
  AbstractControl,
  ValidationErrors,
  ValidatorFn,
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
  AutoCompleteModule,
  AutoCompleteCompleteEvent,
  AutoCompleteSelectEvent,
} from 'primeng/autocomplete';
import {
  ToastService,
  ErrorHandlingService,
} from '@longhl104/pawfect-match-ng';
import { AddressInputComponent, AddressDetails } from 'shared/components';
import {
  AdopterRegistrationRequest,
  AdoptersService,
} from 'shared/services/adopters.service';
import {
  COUNTRY_CODES,
  CountryCode,
} from '../../../../shared/data/country-codes';
import { PhoneNumberService } from '../../../../shared/services/phone-number.service';
import { AppGoogleMapsService } from '../../../../shared/services/app-google-maps.service';
import { environment } from 'environments/environment';

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
    AutoCompleteModule,
  ],
  templateUrl: './registration.html',
  styleUrl: './registration.scss',
})
export class Registration implements OnInit {
  private formBuilder = inject(FormBuilder);
  private router = inject(Router);
  private adoptersService = inject(AdoptersService);
  private toastService = inject(ToastService);
  private errorHandlingService = inject(ErrorHandlingService);
  private appGoogleMaps = inject(AppGoogleMapsService);

  registrationForm: FormGroup;
  isSubmitting = false;
  selectedAddress: AddressDetails | null = null;

  // Country codes for phone numbers
  countryCodes: CountryCode[] = COUNTRY_CODES;
  filteredCountryCodes: CountryCode[] = [];

  constructor() {
    this.registrationForm = this.createForm();
  }

  ngOnInit(): void {
    this.tryEnhanceCountryCodeWithGeolocation();
  }

  onAddressSelected(addressDetails: AddressDetails | null): void {
    this.selectedAddress = addressDetails;
  }

  // Try to improve default country using browser geolocation + Google Maps Geocoder (if loaded)
  private async tryEnhanceCountryCodeWithGeolocation(): Promise<void> {
    try {
      if (
        typeof navigator !== 'undefined' &&
        typeof window !== 'undefined' &&
        'geolocation' in navigator
      ) {
        await this.appGoogleMaps.ensureLoaded(environment.googleMapsApiKey);
        navigator.geolocation.getCurrentPosition(
          (pos) => {
            const lat = pos.coords.latitude;
            const lng = pos.coords.longitude;
            this.appGoogleMaps.reverseGeocodeCountry(lat, lng).then((region: string | null) => {
              if (!region) return;
              const codeNum =
                PhoneNumberService.getCountryCodeForRegion(region);
              const codeStr = codeNum ? `+${codeNum}` : null;
              if (
                codeStr &&
                this.countryCodes.some((cc) => cc.value === codeStr) &&
                this.registrationForm.get('countryCode')?.value !== codeStr
              ) {
                this.registrationForm.get('countryCode')?.setValue(codeStr);
                const phoneCtrl = this.registrationForm.get('phoneNumber');
                if (phoneCtrl?.value) {
                  phoneCtrl.updateValueAndValidity({ emitEvent: false });
                }
              }
            });
          },
          () => {
            // ignore errors, fallback remains
          },
          { enableHighAccuracy: false, timeout: 5000, maximumAge: 300000 },
        );
      }
    } catch {
      // ignore; fallback remains
    }
  }

  private createForm(): FormGroup {
    const defaultCountryCode = this.getDefaultCountryCodeFromLocale() ?? '+1';
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
        countryCode: [defaultCountryCode, []], // Default based on locale (fallback +1)
        phoneNumber: ['', []],
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

    // Apply phone number validator after form is created (avoids early undefined context)
    form.get('phoneNumber')?.setValidators(this.createPhoneNumberValidator());

    // Re-validate phone number when country code changes
    form.get('countryCode')?.valueChanges.subscribe(() => {
      const phoneNumberControl = form.get('phoneNumber');
      if (phoneNumberControl?.value) {
        phoneNumberControl.updateValueAndValidity({ emitEvent: false });
      }
    });

    return form;
  }

  // Determine default calling code from user's locale/region
  private getDefaultCountryCodeFromLocale(): string | null {
    const region = this.getDefaultRegionFromLocale();
    if (!region) return null;
    const codeNum = PhoneNumberService.getCountryCodeForRegion(region);
    const codeStr = codeNum ? `+${codeNum}` : null;
    // Ensure the code exists in our list
    return codeStr && this.countryCodes.some((cc) => cc.value === codeStr)
      ? codeStr
      : null;
  }

  // Best-effort region detection from browser locale
  private getDefaultRegionFromLocale(): string | null {
    try {
      const lang =
        (typeof navigator !== 'undefined' &&
          ((navigator as Navigator & { languages?: string[] }).languages?.[0] ||
            navigator.language)) ||
        '';

      const locale =
        lang ||
        (typeof Intl !== 'undefined'
          ? Intl.DateTimeFormat().resolvedOptions().locale
          : '');

      if (locale) {
        const parts = locale.split(/[-_]/);
        const maybeRegion = parts.length > 1 ? parts[parts.length - 1] : '';
        if (maybeRegion && maybeRegion.length === 2)
          return maybeRegion.toUpperCase();
      }
    } catch {
      // ignore and fallback
    }
    return null;
  }

  // AutoComplete filter for country codes
  filterCountryCodes(event: AutoCompleteCompleteEvent): void {
    const query = (event?.query ?? '').toLowerCase();
    if (!query) {
      this.filteredCountryCodes = this.countryCodes;
      return;
    }

    this.filteredCountryCodes = this.countryCodes.filter(
      (cc) =>
        cc.label.toLowerCase().includes(query) ||
        cc.value.toLowerCase().includes(query),
    );
  }

  // Ensure the form control stores just the calling code string (e.g., "+1")
  onCountrySelect(event: AutoCompleteSelectEvent): void {
    const selected = event?.value as CountryCode | string;
    const code = typeof selected === 'string' ? selected : selected?.value;
    if (code) {
      this.registrationForm.get('countryCode')?.setValue(code);
    }
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

  // Custom validator for phone numbers using google-libphonenumber
  private createPhoneNumberValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (!value) return null; // Optional field

      // Get country code from sibling control safely
      const parent = control.parent as FormGroup | null;
      const countryCode = parent?.get('countryCode')?.value || '+1';

      const countryCodeEntry = this.countryCodes.find(
        (cc) => cc.value === countryCode,
      );

      if (!countryCodeEntry) {
        return { pattern: true };
      }

      // Validate using google-libphonenumber
      const isValid = PhoneNumberService.validatePhoneNumber(
        value,
        countryCodeEntry.regionCode,
      );

      return isValid ? null : { pattern: true };
    };
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

  getCompletePhoneNumber(): string {
    const countryCode = this.registrationForm.get('countryCode')?.value || '';
    const phoneNumber = this.registrationForm.get('phoneNumber')?.value || '';

    if (!phoneNumber) return '';

    // Get the region code for proper formatting
    const countryCodeEntry = this.countryCodes.find(
      (cc) => cc.value === countryCode,
    );
    if (!countryCodeEntry) {
      return `${countryCode}${phoneNumber}`;
    }

    // Use google-libphonenumber to format the complete number
    const completeNumber = `${countryCode}${phoneNumber}`;
    const validationResult =
      PhoneNumberService.parseAndValidatePhoneNumber(completeNumber);

    return validationResult.isValid && validationResult.formattedNumber
      ? validationResult.formattedNumber
      : completeNumber;
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

      // Include the detailed address information
      const finalData: AdopterRegistrationRequest = {
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        password: formData.password,
        phoneNumber: this.getCompletePhoneNumber(),
        bio: formData.bio,
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
