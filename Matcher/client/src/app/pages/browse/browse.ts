import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  signal,
  ElementRef,
  NgZone,
  inject,
  viewChild,
  OnDestroy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { SelectModule } from 'primeng/select';
import { SkeletonModule } from 'primeng/skeleton';
import { SliderModule } from 'primeng/slider';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { DividerModule } from 'primeng/divider';
import {
  SpeciesApi,
  PetSpeciesDto,
  PetBreedDto,
} from 'shared/apis/generated-apis';
import { GoogleMapsService } from '@longhl104/pawfect-match-ng';
import { environment } from 'environments/environment';
import { firstValueFrom } from 'rxjs';

// Types
interface Pet {
  id: string;
  name: string;
  species: string;
  breed: string;
  age: number;
  gender: string;
  description: string;
  adoptionFee: number;
  location: string;
  distance: number;
  imageUrl: string;
  shelter: string;
  isSpayedNeutered: boolean;
  isGoodWithKids: boolean;
  isGoodWithPets: boolean;
}

interface DropdownOption {
  label: string;
  value: string | null;
}

interface GoogleMapsAddressComponent {
  types: string[];
  long_name: string;
  short_name: string;
}

interface GoogleMapsGeometry {
  location: {
    lat(): number;
    lng(): number;
  };
}

interface GoogleMapsGeocoderResult {
  formatted_address: string;
  address_components: GoogleMapsAddressComponent[];
  geometry: GoogleMapsGeometry;
}

declare const google: {
  maps: {
    Geocoder: new () => {
      geocode: (request: {
        location?: { lat: number; lng: number };
        address?: string;
      }) => Promise<{
        results: GoogleMapsGeocoderResult[];
      }>;
    };
    places: {
      PlaceAutocompleteElement: new ({
        includedRegionCodes,
      }: {
        includedRegionCodes: string[];
      }) => HTMLElement & {
        componentRestrictions: { country: string };
        types: string[];
        addEventListener: (
          event: string,
          callback: (event: Event) => void,
        ) => void;
        getPlace: () => {
          formatted_address?: string;
          address_components?: GoogleMapsAddressComponent[];
          geometry?: GoogleMapsGeometry;
        };
      };
      AutocompleteService: new () => {
        getPlacePredictions: (
          request: {
            input: string;
            types?: string[];
            componentRestrictions?: { country: string[] };
          },
          callback: (
            predictions:
              | {
                  description: string;
                  place_id: string;
                }[]
              | null,
            status: string,
          ) => void,
        ) => void;
      };
      PlacesService: new (map: HTMLDivElement) => {
        getDetails: (
          request: { placeId: string },
          callback: (
            place: {
              formatted_address: string;
              geometry: GoogleMapsGeometry;
            } | null,
            status: string,
          ) => void,
        ) => void;
      };
    };
  };
};

@Component({
  selector: 'app-browse',
  imports: [
    CommonModule,
    FormsModule,
    AutoCompleteModule,
    ButtonModule,
    CardModule,
    InputTextModule,
    PaginatorModule,
    SelectModule,
    SkeletonModule,
    SliderModule,
    TagModule,
    TooltipModule,
    DividerModule,
  ],
  templateUrl: './browse.html',
  styleUrl: './browse.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrowseComponent implements OnInit, OnDestroy {
  private ngZone = inject(NgZone);
  private googleMapsService = inject(GoogleMapsService);
  private speciesApi = inject(SpeciesApi);

  protected readonly Math = Math;

  readonly locationInputRef = viewChild<ElementRef>('locationInput');

  // Search and filters
  searchLocation = signal('');
  currentLocationCoords = signal<{ lat: number; lng: number } | null>(null);
  selectedSpecies = signal<string | null>(null);
  selectedBreed = signal<string | null>(null);
  maxDistance = signal(50); // km
  ageRange = signal([0, 15]); // years

  // Data
  pets = signal<Pet[]>([]);
  filteredPets = signal<Pet[]>([]);
  breeds = signal<DropdownOption[]>([]);
  isLoading = signal(false);
  isLoadingLocation = signal(false);
  isLoadingSpecies = signal(false);
  isLoadingBreeds = signal(false);

  // API Data
  apiSpecies = signal<PetSpeciesDto[]>([]);
  apiBreeds = signal<PetBreedDto[]>([]);

  // Helper methods to get display names
  getSelectedSpeciesName(): string {
    const speciesId = this.selectedSpecies();
    if (!speciesId || speciesId === 'null') return 'All Species';

    const species = this.apiSpecies().find(
      (s) => s.speciesId?.toString() === speciesId,
    );
    return species?.name || 'Unknown Species';
  }

  getSelectedBreedName(): string {
    const breedId = this.selectedBreed();
    if (!breedId || breedId === 'null') return 'All Breeds';

    const breed = this.apiBreeds().find(
      (b) => b.breedId?.toString() === breedId,
    );
    return breed?.name || 'Unknown Breed';
  }

  // Autocomplete properties
  filteredBreeds = signal<DropdownOption[]>([]);
  selectedBreedObject: DropdownOption | string | null = null;

  // Google Places Autocomplete
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private autocomplete: any = null;
  private eventListeners: (() => void)[] = [];

  // Pagination
  currentPage = signal(0);
  pageSize = signal(12);
  totalRecords = signal(0);

  // Dropdown options - these will be populated from API
  speciesOptions = signal<DropdownOption[]>([
    { label: 'All Species', value: null },
  ]);

  // Mock data for demonstration
  mockPets = [
    {
      id: '1',
      name: 'Buddy',
      species: 'Dog',
      breed: 'Golden Retriever',
      age: 3,
      gender: 'Male',
      description: 'Friendly and energetic dog looking for an active family.',
      adoptionFee: 250,
      location: 'Melbourne, VIC',
      distance: 5.2,
      imageUrl:
        'https://images.unsplash.com/photo-1514888286974-6c03e2ca1dba?w=400',
      shelter: 'Melbourne Animal Rescue',
      isSpayedNeutered: true,
      isGoodWithKids: true,
      isGoodWithPets: true,
    },
    {
      id: '2',
      name: 'Luna',
      species: 'Cat',
      breed: 'Siamese',
      age: 2,
      gender: 'Female',
      description: 'Sweet and calm cat perfect for apartment living.',
      adoptionFee: 180,
      location: 'Sydney, NSW',
      distance: 12.8,
      imageUrl:
        'https://images.unsplash.com/photo-1514888286974-6c03e2ca1dba?w=400',
      shelter: 'Sydney Cat Rescue',
      isSpayedNeutered: true,
      isGoodWithKids: true,
      isGoodWithPets: false,
    },
    {
      id: '3',
      name: 'Charlie',
      species: 'Dog',
      breed: 'Labrador',
      age: 1,
      gender: 'Male',
      description: 'Playful puppy who loves to fetch and swim.',
      adoptionFee: 300,
      location: 'Brisbane, QLD',
      distance: 8.5,
      imageUrl:
        'https://images.unsplash.com/photo-1583337130417-3346a1be7dee?w=400',
      shelter: 'Brisbane Pet Haven',
      isSpayedNeutered: false,
      isGoodWithKids: true,
      isGoodWithPets: true,
    },
    {
      id: '4',
      name: 'Mittens',
      species: 'Cat',
      breed: 'Persian',
      age: 5,
      gender: 'Female',
      description: 'Gentle senior cat looking for a quiet home.',
      adoptionFee: 150,
      location: 'Perth, WA',
      distance: 15.3,
      imageUrl:
        'https://images.unsplash.com/photo-1573865526739-10659fec78a5?w=400',
      shelter: 'Perth Animal Welfare',
      isSpayedNeutered: true,
      isGoodWithKids: false,
      isGoodWithPets: false,
    },
  ];

  ngOnInit() {
    this.loadSpecies();
    this.initializeMockData();
    this.getCurrentLocation();
    this.initializeGoogleMapsAndAutocomplete();
  }

  private async initializeGoogleMapsAndAutocomplete() {
    try {
      await this.googleMapsService.loadGoogleMaps(environment.googleMapsApiKey);
      // Wait for view to be ready then initialize autocomplete
      setTimeout(() => this.initializeLocationAutocomplete(), 100);
    } catch (error) {
      console.error('Failed to load Google Maps:', error);
    }
  }

  private async loadSpecies() {
    try {
      this.isLoadingSpecies.set(true);
      const response = await firstValueFrom(this.speciesApi.species());

      if (response?.success && response.species) {
        this.apiSpecies.set(response.species);

        // Convert API species to dropdown options
        const speciesOptions: DropdownOption[] = [
          { label: 'All Species', value: null },
          ...response.species.map((species) => ({
            label: species.name || '',
            value: species.speciesId?.toString() || '',
          })),
        ];

        this.speciesOptions.set(speciesOptions);
      }
    } catch (error) {
      console.error('Failed to load species:', error);
    } finally {
      this.isLoadingSpecies.set(false);
    }
  }

  private async loadBreeds(speciesId: number) {
    try {
      this.isLoadingBreeds.set(true);
      const response = await firstValueFrom(this.speciesApi.breeds(speciesId));

      if (response?.success && response.breeds) {
        this.apiBreeds.set(response.breeds);

        // Convert API breeds to dropdown options
        const breedOptions: DropdownOption[] = [
          { label: 'All Breeds', value: null },
          ...response.breeds.map((breed) => ({
            label: breed.name || '',
            value: breed.breedId?.toString() || '',
          })),
        ];

        this.breeds.set(breedOptions);
        this.filteredBreeds.set(breedOptions);
      }
    } catch (error) {
      console.error('Failed to load breeds:', error);
      // Set default breeds on error
      const defaultBreeds = [{ label: 'All Breeds', value: null }];
      this.breeds.set(defaultBreeds);
      this.filteredBreeds.set(defaultBreeds);
    } finally {
      this.isLoadingBreeds.set(false);
    }
  }

  ngOnDestroy(): void {
    // Clean up event listeners
    this.eventListeners.forEach((removeListener) => removeListener());

    // Clean up autocomplete element if it exists
    if (this.autocomplete) {
      this.autocomplete.remove();
    }
  }

  private initializeLocationAutocomplete() {
    const locationInputRef = this.locationInputRef();
    if (!locationInputRef?.nativeElement) {
      setTimeout(() => this.initializeLocationAutocomplete(), 100);
      return;
    }

    if (typeof google === 'undefined' || !google.maps?.places) {
      console.warn('Google Maps Places API not available');
      return;
    }

    // Override attachShadow to enable styling of Google PlaceAutocompleteElement
    const attachShadow = Element.prototype.attachShadow;
    Element.prototype.attachShadow = function (init: ShadowRootInit) {
      // Check if we are the new Google places autocomplete element...
      if (this.localName === 'gmp-place-autocomplete') {
        // If we are, we need to override the default behaviour of attachShadow() to
        // set the mode to open to allow us to crowbar a style element into the shadow DOM.
        const shadow = attachShadow.call(this, {
          ...init,
          mode: 'open',
        });

        const style = document.createElement('style');

        // Apply our own styles to the shadow DOM.
        style.textContent = `
          input {
            color: var(--p-inputtext-color);
          }

          .focus-ring {
            border: 2px solid var(--p-primary-color) !important;
          }
        `;

        shadow.appendChild(style);

        // Set the shadowRoot property to the new shadow root that has our styles in it.
        return shadow;
      }
      // ...for other elements, proceed with the original behaviour of attachShadow().
      return attachShadow.call(this, init);
    };

    // Create the PlaceAutocompleteElement
    const autocompleteElement = new google.maps.places.PlaceAutocompleteElement(
      {
        includedRegionCodes: ['au'],
      },
    );

    // Replace the input with the autocomplete element
    const parentElement = locationInputRef.nativeElement.parentElement;
    if (parentElement) {
      // Copy styles and attributes from the original input
      autocompleteElement.className = locationInputRef.nativeElement.className;
      autocompleteElement.style.padding = '0';
      autocompleteElement.id = locationInputRef.nativeElement.id;

      autocompleteElement.setAttribute('placeholder', 'Enter your location...');

      // Replace the input element
      parentElement.replaceChild(
        autocompleteElement,
        locationInputRef.nativeElement,
      );

      // Store reference to the new element
      this.autocomplete = autocompleteElement;

      // Listen for place selection
      autocompleteElement.addEventListener(
        'gmp-select',
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        async ({ placePrediction }: any) => {
          this.ngZone.run(async () => {
            const place = placePrediction.toPlace();
            await place.fetchFields({
              fields: ['displayName', 'formattedAddress', 'location'],
            });

            const placeData = place.toJSON();

            if (!placeData.formattedAddress) {
              return;
            }

            this.searchLocation.set(placeData.formattedAddress);

            if (placeData.location) {
              this.currentLocationCoords.set({
                lat: placeData.location.lat,
                lng: placeData.location.lng,
              });
            }

            this.updatePetDistances();
            this.applyFilters();
          });
        },
      );

      const clearButton = this.getAutoCompleteClearButton();
      if (clearButton) {
        clearButton.addEventListener('click', () => {
          this.searchLocation.set('');
          this.currentLocationCoords.set(null);
          this.applyFilters();
        });
      }
    }
  }

  private initializeMockData() {
    this.pets.set(this.mockPets);
    this.filteredPets.set(this.mockPets);
    this.totalRecords.set(this.mockPets.length);
    // Initialize breeds with default "All Breeds" option
    const defaultBreeds = [{ label: 'All Breeds', value: null }];
    this.breeds.set(defaultBreeds);
    this.filteredBreeds.set(defaultBreeds);
  }

  getCurrentLocation() {
    if (!navigator.geolocation) {
      console.warn('Geolocation is not supported by this browser.');
      return;
    }

    this.isLoadingLocation.set(true);

    navigator.geolocation.getCurrentPosition(
      (position) => {
        const coords = {
          lat: position.coords.latitude,
          lng: position.coords.longitude,
        };
        this.currentLocationCoords.set(coords);
        this.reverseGeocode(coords);
        this.isLoadingLocation.set(false);
      },
      (error) => {
        console.warn('Error getting location:', error);
        this.isLoadingLocation.set(false);
      },
      {
        enableHighAccuracy: true,
        timeout: 10000,
        maximumAge: 300000, // 5 minutes
      },
    );
  }

  private async reverseGeocode(coords: { lat: number; lng: number }) {
    try {
      if (typeof google === 'undefined') return;

      const geocoder = new google.maps.Geocoder();
      const response = await geocoder.geocode({
        location: coords,
      });

      if (response.results && response.results.length > 0) {
        const result = response.results[0];

        // Use the full formatted address instead of extracting components
        this.searchLocation.set(result.formatted_address);

        const inputElement = this.getAutoCompleteInputElement();
        if (inputElement) {
          inputElement.value = result.formatted_address;
        }
      }
    } catch (error) {
      console.error('Reverse geocoding failed:', error);
    }
  }

  private getAutoCompleteInputElement(): HTMLInputElement | null {
    for (const [, value] of Object.entries(this.autocomplete)) {
      if (value instanceof HTMLInputElement) {
        return value;
      }
    }

    return null;
  }

  private getAutoCompleteClearButton(): HTMLButtonElement | null {
    const inputElement = this.getAutoCompleteInputElement();
    if (!inputElement) return null;

    const siblings = Array.from(inputElement.parentElement?.children || []);
    return siblings.find((sibling) =>
      sibling.classList.contains('clear-button'),
    ) as HTMLButtonElement | null;
  }

  onLocationChange() {
    // This method is now handled by Google Places Autocomplete
    // We can keep it for manual input validation if needed
    this.applyFilters();
  }

  private updatePetDistances() {
    const currentCoords = this.currentLocationCoords();
    if (!currentCoords) return;

    // Update distances for mock pets (in a real app, this would be an API call)
    const updatedPets = this.pets().map((pet) => {
      // For mock data, generate random distances based on location
      // In a real app, you'd calculate actual distances or get them from API
      const mockDistance = Math.round(Math.random() * 50 + 5); // 5-55km
      return { ...pet, distance: mockDistance };
    });

    this.pets.set(updatedPets);
    this.applyFilters();
  }

  onSpeciesChange() {
    const selectedSpecies = this.selectedSpecies();

    // Reset breed selection when species changes
    this.selectedBreed.set(null);
    this.selectedBreedObject = null;

    if (selectedSpecies && selectedSpecies !== 'null') {
      // Load breeds for the selected species from API
      const speciesId = parseInt(selectedSpecies, 10);
      if (!isNaN(speciesId)) {
        this.loadBreeds(speciesId);
      }
    } else {
      // If no species selected, clear breeds
      const defaultBreeds = [{ label: 'All Breeds', value: null }];
      this.breeds.set(defaultBreeds);
      this.filteredBreeds.set(defaultBreeds);
    }

    this.applyFilters();
  }

  onBreedChange() {
    // Debug logging to see what we're receiving
    console.log('onBreedChange called with selectedBreedObject:', this.selectedBreedObject, typeof this.selectedBreedObject);

    // Handle both object and string cases
    if (this.selectedBreedObject) {
      if (typeof this.selectedBreedObject === 'string') {
        // If it's a string, it's likely the breed ID, so find the actual object
        const breedId = this.selectedBreedObject;
        const breedObject = this.breeds().find(breed => breed.value === breedId);
        if (breedObject) {
          this.selectedBreedObject = breedObject;
          this.selectedBreed.set(breedObject.value);
        } else {
          // Fallback: treat the string as the breed ID
          this.selectedBreed.set(breedId);
        }
      } else if (typeof this.selectedBreedObject === 'object' && this.selectedBreedObject.value !== undefined) {
        // If it's an object with a value property (correct case)
        this.selectedBreed.set(this.selectedBreedObject.value);
      } else {
        console.warn('Unexpected selectedBreedObject format:', this.selectedBreedObject);
        this.selectedBreed.set(null);
      }
    } else {
      this.selectedBreed.set(null);
    }
    this.applyFilters();
  }

  clearBreedSelection() {
    console.log('clearBreedSelection called');
    this.selectedBreed.set(null);
    this.selectedBreedObject = null;
    this.applyFilters();
  }

  searchBreeds(event: { query: string }) {
    console.log('searchBreeds called with query:', event.query);
    const query = event.query.toLowerCase();
    const availableBreeds = this.breeds();
    console.log('Available breeds:', availableBreeds);

    if (query === null || query === undefined) {
      this.filteredBreeds.set(availableBreeds);
    } else {
      const filtered = availableBreeds.filter((breed) =>
        breed.label.toLowerCase().includes(query),
      );

      this.filteredBreeds.set(filtered);
    }
    console.log('Filtered breeds set to:', this.filteredBreeds());
  }

  onDistanceChange() {
    this.applyFilters();
  }

  onAgeRangeChange() {
    this.applyFilters();
  }

  private applyFilters() {
    let filtered = [...this.pets()];

    // Species filter
    const speciesId = this.selectedSpecies();
    if (speciesId && speciesId !== 'null') {
      // Find the species name from the API data for filtering against mock data
      const selectedSpeciesObj = this.apiSpecies().find(
        (s) => s.speciesId?.toString() === speciesId,
      );
      if (selectedSpeciesObj?.name) {
        filtered = filtered.filter(
          (pet) => pet.species === selectedSpeciesObj.name,
        );
      }
    }

    // Breed filter
    const breedId = this.selectedBreed();
    if (breedId && breedId !== 'null') {
      // Find the breed name from the API data for filtering against mock data
      const selectedBreedObj = this.apiBreeds().find(
        (b) => b.breedId?.toString() === breedId,
      );
      if (selectedBreedObj?.name) {
        filtered = filtered.filter(
          (pet) => pet.breed === selectedBreedObj.name,
        );
      }
    }

    // Age range filter
    const ageRange = this.ageRange();
    filtered = filtered.filter(
      (pet) => pet.age >= ageRange[0] && pet.age <= ageRange[1],
    );

    // Distance filter (mock implementation)
    const maxDist = this.maxDistance();
    filtered = filtered.filter((pet) => pet.distance <= maxDist);

    this.filteredPets.set(filtered);
    this.totalRecords.set(filtered.length);
    this.currentPage.set(0); // Reset to first page
  }

  clearFilters() {
    this.selectedSpecies.set(null);
    this.selectedBreed.set(null);
    this.selectedBreedObject = null;
    this.maxDistance.set(50);
    this.ageRange.set([0, 15]);
    this.searchLocation.set('');

    // Clear the autocomplete element value if it exists
    if (this.autocomplete && 'value' in this.autocomplete) {
      (this.autocomplete as HTMLInputElement).value = '';
    }

    this.filteredPets.set(this.pets());
    this.totalRecords.set(this.pets().length);
    this.currentPage.set(0);

    // Reset breeds to default when clearing all filters
    const defaultBreeds = [{ label: 'All Breeds', value: null }];
    this.breeds.set(defaultBreeds);
    this.filteredBreeds.set(defaultBreeds);
  }

  getActiveFiltersCount(): number {
    let count = 0;
    if (this.selectedSpecies()) count++;
    if (this.selectedBreed()) count++;
    if (this.searchLocation()) count++;
    if (this.maxDistance() !== 50) count++;
    if (this.ageRange()[0] !== 0 || this.ageRange()[1] !== 15) count++;
    return count;
  }

  onPageChange(event: PaginatorState) {
    this.currentPage.set(event.page ?? 0);
  }

  getPaginatedPets() {
    const startIndex = this.currentPage() * this.pageSize();
    const endIndex = startIndex + this.pageSize();
    return this.filteredPets().slice(startIndex, endIndex);
  }

  onPetClick(pet: Pet) {
    // TODO: Navigate to pet detail page or show more info
    console.log('Pet clicked:', pet);
  }

  onContactShelter(pet: Pet, event: Event) {
    event.stopPropagation();
    // TODO: Implement contact shelter functionality
    console.log('Contact shelter for pet:', pet);
  }

  onFavorite(pet: Pet, event: Event) {
    event.stopPropagation();
    // TODO: Implement favorite functionality
    console.log('Favorite pet:', pet);
  }
}
