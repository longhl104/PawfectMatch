import {
  Component,
  OnInit,
  NgZone,
  ViewChild,
  ElementRef,
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
import { environment } from 'environments/environment';

declare const google: any;

@Component({
  selector: 'app-registration',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './registration.html',
  styleUrl: './registration.scss',
})
export class Registration implements OnInit {
  @ViewChild('addressInput', { static: false }) addressInputRef!: ElementRef;

  registrationForm: FormGroup;
  showPassword = false;
  isSubmitting = false;

  private autocomplete: any;
  selectedAddress: any = null;

  constructor(
    private formBuilder: FormBuilder,
    private router: Router,
    private ngZone: NgZone
  ) {
    this.registrationForm = this.createForm();
  }

  ngOnInit() {
    this.loadGoogleMapsScript();
  }

  private loadGoogleMapsScript() {
    if (typeof google !== 'undefined') {
      this.initializeAutocomplete();
      return;
    }

    const script = document.createElement('script');
    script.src = `https://maps.googleapis.com/maps/api/js?key=${environment.googleMapsApiKey}&libraries=places`;
    script.async = true;
    script.defer = true;
    script.onload = () => {
      this.initializeAutocomplete();
    };
    document.head.appendChild(script);
  }

  private initializeAutocomplete() {
    if (!this.addressInputRef?.nativeElement) {
      // If the input isn't ready yet, try again after a short delay
      setTimeout(() => this.initializeAutocomplete(), 100);
      return;
    }

    const options = {
      componentRestrictions: { country: 'AU' }, // Restrict to Australia
      fields: ['formatted_address', 'address_components', 'geometry'],
      types: ['geocode'], // Only return geocoding results
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
          return;
        }

        this.selectedAddress = this.extractAddressComponents(place);
        this.registrationForm.get('address')?.setValue(place.formatted_address);
        this.registrationForm.get('address')?.setErrors(null);
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
        address: ['', [Validators.required]], // Single address field
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
    if (this.registrationForm.valid && this.selectedAddress) {
      this.isSubmitting = true;

      try {
        const formData = this.registrationForm.value;
        const { confirmPassword, ...registrationData } = formData;

        // Include the detailed address information
        const finalData = {
          ...registrationData,
          addressDetails: this.selectedAddress,
          location: {
            latitude: this.selectedAddress.latitude,
            longitude: this.selectedAddress.longitude,
          },
        };

        console.log('Registration data:', finalData);

        // TODO: Call registration service
        await new Promise((resolve) => setTimeout(resolve, 2000));

        this.router.navigate(['/auth/login'], {
          queryParams: { message: 'Registration successful! Please log in.' },
        });
      } catch (error) {
        console.error('Registration failed:', error);
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

  goBack(): void {
    this.router.navigate(['/auth/choice']);
  }
}
