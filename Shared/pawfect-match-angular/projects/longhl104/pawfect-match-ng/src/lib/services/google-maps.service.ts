/* eslint-disable @typescript-eslint/no-explicit-any */
import { Injectable } from '@angular/core';

let google: any; // Declare google globally

@Injectable({
  providedIn: 'root',
})
export class GoogleMapsService {
  private isLoaded = false;
  private loadingPromise: Promise<void> | null = null;

  async loadGoogleMaps(apiKey: string): Promise<void> {
    if (this.isLoaded) {
      return Promise.resolve();
    }

    if (this.loadingPromise) {
      return this.loadingPromise;
    }

    this.loadingPromise = new Promise((resolve, reject) => {
      if (typeof google !== 'undefined' && google.maps && google.maps.places) {
        this.isLoaded = true;
        resolve();
        return;
      }

      const callbackName = 'googleMapsCallback' + Date.now();
      (window as any)[callbackName] = () => {
        this.isLoaded = true;
        delete (window as any)[callbackName];
        resolve();
      };

      const script = document.createElement('script');
      script.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&libraries=places&loading=async&callback=${callbackName}`;
      script.async = true;
      script.defer = true;
      script.onerror = () => {
        delete (window as any)[callbackName];
        reject(new Error('Failed to load Google Maps API'));
      };

      document.head.appendChild(script);
    });

    return this.loadingPromise;
  }

  /** Ensure the Maps JS API is loaded (alias for loadGoogleMaps). */
  ensureLoaded(apiKey: string): Promise<void> {
    return this.loadGoogleMaps(apiKey);
  }

  /** Reverse geocode to a 2-letter country/region code (e.g., 'US'), if Maps is available. */
  async reverseGeocodeCountry(lat: number, lng: number): Promise<string | null> {
    try {
      if (typeof google === 'undefined' || !google.maps?.Geocoder) {
        return null;
      }
      const geocoder = new google.maps.Geocoder();
      const response = await geocoder.geocode({ location: { lat, lng } });
      const results = response?.results as
        | { address_components?: { types?: string[]; short_name?: string }[] }[]
        | undefined;
      if (!results || results.length === 0) return null;
      for (const r of results) {
        const comps = r.address_components || [];
        for (const c of comps) {
          if (
            (c.types || []).includes('country') &&
            c.short_name &&
            c.short_name.length === 2
          ) {
            return c.short_name.toUpperCase();
          }
        }
      }
      return null;
    } catch {
      return null;
    }
  }
}
