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

import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { SkeletonModule } from 'primeng/skeleton';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { DividerModule } from 'primeng/divider';
import { GoogleMapsService } from '@longhl104/pawfect-match-ng';
import { environment } from '../../../environments/environment';

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
    ButtonModule,
    CardModule,
    InputTextModule,
    PaginatorModule,
    SkeletonModule,
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

  // Google Places Autocomplete
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private autocomplete: any = null;
  private eventListeners: (() => void)[] = [];

  // Pagination
  currentPage = signal(0);
  pageSize = signal(12);
  totalRecords = signal(0);

  // Dropdown options
  speciesOptions = signal<DropdownOption[]>([
    { label: 'All Species', value: null },
    { label: 'Dog', value: 'Dog' },
    { label: 'Cat', value: 'Cat' },
    { label: 'Bird', value: 'Bird' },
    { label: 'Rabbit', value: 'Rabbit' },
    { label: 'Other', value: 'Other' },
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
      autocompleteElement.addEventListener('gmp-placeselect', () => {
        this.ngZone.run(() => {
          const place = autocompleteElement.getPlace();

          if (!place.formatted_address) {
            return;
          }

          this.searchLocation.set(place.formatted_address);

          if (place.geometry?.location) {
            this.currentLocationCoords.set({
              lat: place.geometry.location.lat(),
              lng: place.geometry.location.lng(),
            });
          }

          this.updatePetDistances();
          this.applyFilters();
        });
      });
    }
  }

  private initializeMockData() {
    this.pets.set(this.mockPets);
    this.filteredPets.set(this.mockPets);
    this.totalRecords.set(this.mockPets.length);
    this.updateBreedOptions();
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
        const addressComponents = result.address_components;

        // Extract suburb and city
        const suburb = addressComponents.find(
          (comp: GoogleMapsAddressComponent) =>
            comp.types.includes('locality') ||
            comp.types.includes('sublocality'),
        )?.long_name;

        const city = addressComponents.find(
          (comp: GoogleMapsAddressComponent) =>
            comp.types.includes('administrative_area_level_1'),
        )?.short_name;

        const locationString =
          suburb && city ? `${suburb}, ${city}` : result.formatted_address;
        this.searchLocation.set(locationString);
      }
    } catch (error) {
      console.error('Reverse geocoding failed:', error);
    }
  }

  onLocationChange() {
    // This method is now handled by Google Places Autocomplete
    // We can keep it for manual input validation if needed
    this.applyFilters();
  }

  private geocodeTimeout: ReturnType<typeof setTimeout> | null = null;

  private async geocodeLocation(address: string) {
    try {
      if (typeof google === 'undefined') {
        console.warn('Google Maps not loaded');
        return;
      }

      const geocoder = new google.maps.Geocoder();
      const response = await geocoder.geocode({
        address: address,
      });

      if (response.results && response.results.length > 0) {
        const result = response.results[0];
        const location = result.geometry.location;

        // Update current location coordinates
        this.currentLocationCoords.set({
          lat: location.lat(),
          lng: location.lng(),
        });

        // Update the display location if it's more specific
        const formattedAddress = result.formatted_address;
        if (formattedAddress !== address) {
          // Only update if the user hasn't typed something else while geocoding
          if (this.searchLocation().trim() === address) {
            this.searchLocation.set(formattedAddress);
          }
        }

        // Re-calculate distances for mock pets based on new location
        this.updatePetDistances();
      }
    } catch (error) {
      console.error('Geocoding failed:', error);
    }
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
    this.updateBreedOptions();
    this.applyFilters();
  }

  onBreedChange() {
    this.applyFilters();
  }

  onDistanceChange() {
    this.applyFilters();
  }

  onAgeRangeChange() {
    this.applyFilters();
  }

  private updateBreedOptions() {
    const selectedSpecies = this.selectedSpecies();
    let availableBreeds: DropdownOption[] = [
      { label: 'All Breeds', value: null },
    ];

    if (selectedSpecies === 'Dog') {
      availableBreeds = [
        { label: 'All Breeds', value: null },
        { label: 'Golden Retriever', value: 'Golden Retriever' },
        { label: 'Labrador', value: 'Labrador' },
        { label: 'German Shepherd', value: 'German Shepherd' },
        { label: 'French Bulldog', value: 'French Bulldog' },
        { label: 'Poodle', value: 'Poodle' },
        { label: 'Bulldog', value: 'Bulldog' },
        { label: 'Beagle', value: 'Beagle' },
        { label: 'Rottweiler', value: 'Rottweiler' },
        { label: 'Yorkshire Terrier', value: 'Yorkshire Terrier' },
        { label: 'Mixed Breed', value: 'Mixed Breed' },
      ];
    } else if (selectedSpecies === 'Cat') {
      availableBreeds = [
        { label: 'All Breeds', value: null },
        { label: 'Siamese', value: 'Siamese' },
        { label: 'Persian', value: 'Persian' },
        { label: 'Maine Coon', value: 'Maine Coon' },
        { label: 'British Shorthair', value: 'British Shorthair' },
        { label: 'American Shorthair', value: 'American Shorthair' },
        { label: 'Ragdoll', value: 'Ragdoll' },
        { label: 'Bengal', value: 'Bengal' },
        { label: 'Russian Blue', value: 'Russian Blue' },
        { label: 'Domestic Shorthair', value: 'Domestic Shorthair' },
        { label: 'Mixed Breed', value: 'Mixed Breed' },
      ];
    } else if (selectedSpecies === 'Bird') {
      availableBreeds = [
        { label: 'All Breeds', value: null },
        { label: 'Cockatiel', value: 'Cockatiel' },
        { label: 'Budgerigar', value: 'Budgerigar' },
        { label: 'Canary', value: 'Canary' },
        { label: 'Lovebird', value: 'Lovebird' },
        { label: 'Cockatoo', value: 'Cockatoo' },
        { label: 'Parakeet', value: 'Parakeet' },
      ];
    } else if (selectedSpecies === 'Rabbit') {
      availableBreeds = [
        { label: 'All Breeds', value: null },
        { label: 'Holland Lop', value: 'Holland Lop' },
        { label: 'Netherland Dwarf', value: 'Netherland Dwarf' },
        { label: 'Mini Rex', value: 'Mini Rex' },
        { label: 'Lionhead', value: 'Lionhead' },
        { label: 'Flemish Giant', value: 'Flemish Giant' },
        { label: 'Mixed Breed', value: 'Mixed Breed' },
      ];
    }

    this.breeds.set(availableBreeds);

    // Reset breed selection if current breed is not available for new species
    const currentBreed = this.selectedBreed();
    if (
      currentBreed &&
      !availableBreeds.find((breed) => breed.value === currentBreed)
    ) {
      this.selectedBreed.set(null);
    }
  }

  private applyFilters() {
    let filtered = [...this.pets()];

    // Species filter
    const species = this.selectedSpecies();
    if (species) {
      filtered = filtered.filter((pet) => pet.species === species);
    }

    // Breed filter
    const breed = this.selectedBreed();
    if (breed) {
      filtered = filtered.filter((pet) => pet.breed === breed);
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
    this.updateBreedOptions();
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
