/* eslint-disable @typescript-eslint/no-explicit-any */
import { Injectable, inject } from '@angular/core';
import { GoogleMapsService } from '@longhl104/pawfect-match-ng';

declare const google: any;

@Injectable({ providedIn: 'root' })
export class AppGoogleMapsService {
  private base = inject(GoogleMapsService);

  ensureLoaded(apiKey: string): Promise<void> {
    return this.base.loadGoogleMaps(apiKey);
  }

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
