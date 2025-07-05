/* eslint-disable @typescript-eslint/no-explicit-any */
import {
  Component,
  Input,
  Output,
  EventEmitter,
  ViewChild,
  ElementRef,
  NgZone,
  inject,
  afterNextRender,
  forwardRef,
  OnDestroy,
} from '@angular/core';

import {
  ControlValueAccessor,
  NG_VALUE_ACCESSOR,
  FormControl,
  ReactiveFormsModule,
} from '@angular/forms';
import {
  GoogleMapsService,
  ErrorHandlingService,
} from '@longhl104/pawfect-match-ng';
import { environment } from 'environments/environment';

declare const google: any;

export interface AddressDetails {
  streetNumber: string;
  streetName: string;
  suburb: string;
  city: string;
  state: string;
  postcode: string;
  country: string;
  formattedAddress: string;
  latitude?: number;
  longitude?: number;
}

@Component({
  selector: 'app-address-input',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './address-input.component.html',
  styleUrl: './address-input.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => AddressInputComponent),
      multi: true,
    },
  ],
})
export class AddressInputComponent implements ControlValueAccessor, OnDestroy {
  private ngZone = inject(NgZone);
  private googleMapsService = inject(GoogleMapsService);
  private errorHandlingService = inject(ErrorHandlingService);

  @ViewChild('addressInput', { static: false }) addressInputRef!: ElementRef;

  @Input() label = 'Address';
  @Input() placeholder = 'Enter your full address';
  @Input() required = false;
  @Input() readonly = false;
  @Input() id = 'address';
  @Input() showHelperText = true;
  @Input() errorMessages: Record<string, string> = {
    required: 'Address is required',
    invalidAddress: 'Please select a valid address from the suggestions',
  };

  private _disabled = false;
  @Input()
  set disabled(value: boolean) {
    this._disabled = value;
    this.updateDisabledState();
  }
  get disabled(): boolean {
    return this._disabled;
  }

  @Output() addressSelected = new EventEmitter<AddressDetails | null>();
  @Output() validationChange = new EventEmitter<Record<string, any> | null>();

  addressControl = new FormControl('');
  isGoogleMapsLoading = true;
  selectedAddress: AddressDetails | null = null;

  private autocomplete: any;
  private eventListeners: (() => void)[] = [];

  // ControlValueAccessor implementation
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  private onChange = (_value: string) => {
    // Implementation provided by Angular forms
  };
  private onTouched = () => {
    // Implementation provided by Angular forms
  };

  constructor() {
    // Initialize with disabled state based on input properties
    this.updateDisabledState();

    afterNextRender(async () => {
      try {
        await this.googleMapsService.loadGoogleMaps(
          environment.googleMapsApiKey,
        );

        this.isGoogleMapsLoading = false;
        this.updateDisabledState();

        // Wait for view to be ready
        setTimeout(() => this.initializeAutocomplete(), 100);
      } catch (error) {
        console.error('Failed to load Google Maps:', error);
        this.isGoogleMapsLoading = false;
        this.updateDisabledState();
        this.errorHandlingService.handleErrorWithComponent(
          error,
          this,
          'loadGoogleMaps',
        );
      }
    });

    // Subscribe to control value changes
    this.addressControl.valueChanges.subscribe((value) => {
      this.onChange(value || '');
    });
  }

  ngOnDestroy(): void {
    // Clean up event listeners
    this.eventListeners.forEach((removeListener) => removeListener());
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
      options,
    );

    this.autocomplete.addListener('place_changed', () => {
      this.ngZone.run(() => {
        const place = this.autocomplete.getPlace();

        if (!place.address_components) {
          this.setValidationError({ invalidAddress: true });
          this.selectedAddress = null;
          this.addressSelected.emit(null);
          return;
        }

        this.selectedAddress = this.extractAddressComponents(place);
        this.addressControl.setValue(place.formatted_address);
        this.setValidationError(null);
        this.addressSelected.emit(this.selectedAddress);
      });
    });

    // Add input event listener to validate manual typing
    const inputListener = (event: any) => {
      this.ngZone.run(() => {
        const currentValue = event.target.value;

        // If user is typing and the current value doesn't match our selected address,
        // mark as invalid
        if (
          currentValue &&
          (!this.selectedAddress ||
            this.selectedAddress.formattedAddress !== currentValue)
        ) {
          this.setValidationError({ invalidAddress: true });
        }

        // If field is empty, only show required error if field is required
        if (!currentValue) {
          this.selectedAddress = null;
          this.addressSelected.emit(null);
          if (this.required) {
            this.setValidationError({ required: true });
          } else {
            this.setValidationError(null);
          }
        }
      });
    };

    // Add blur event to validate when user leaves the field
    const blurListener = () => {
      this.ngZone.run(() => {
        this.onTouched();
        const currentValue = this.addressControl.value;

        if (
          currentValue &&
          (!this.selectedAddress ||
            this.selectedAddress.formattedAddress !== currentValue)
        ) {
          this.setValidationError({ invalidAddress: true });
        }
      });
    };

    this.addressInputRef.nativeElement.addEventListener('input', inputListener);
    this.addressInputRef.nativeElement.addEventListener('blur', blurListener);

    // Store removal functions for cleanup
    this.eventListeners.push(
      () =>
        this.addressInputRef.nativeElement.removeEventListener(
          'input',
          inputListener,
        ),
      () =>
        this.addressInputRef.nativeElement.removeEventListener(
          'blur',
          blurListener,
        ),
    );
  }

  private extractAddressComponents(place: any): AddressDetails {
    const components = place.address_components;
    const result: AddressDetails = {
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

  private setValidationError(errors: Record<string, any> | null) {
    this.validationChange.emit(errors);
  }

  private updateDisabledState(): void {
    if (this._disabled || this.isGoogleMapsLoading) {
      this.addressControl.disable();
    } else {
      this.addressControl.enable();
    }
  }

  // ControlValueAccessor methods
  writeValue(value: string): void {
    this.addressControl.setValue(value, { emitEvent: false });
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this._disabled = isDisabled;
    this.updateDisabledState();
  }

  // Public methods for parent components
  getSelectedAddress(): AddressDetails | null {
    return this.selectedAddress;
  }

  clearAddress(): void {
    this.selectedAddress = null;
    this.addressControl.setValue('');
    this.addressSelected.emit(null);
    this.setValidationError(null);
  }

  // Helper method for displaying validation errors
  hasError(errorType: string): boolean {
    return (
      this.addressControl.hasError(errorType) && this.addressControl.touched
    );
  }

  getErrorMessage(errorType: string): string {
    return this.errorMessages[errorType] || '';
  }
}
