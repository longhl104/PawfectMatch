import { Injectable } from '@angular/core';
import { environment } from 'environments/environment';

let google: any; // Declare google globally

@Injectable({
  providedIn: 'root',
})
export class GoogleMapsService {
  private isLoaded = false;
  private loadingPromise: Promise<void> | null = null;

  async loadGoogleMaps(): Promise<void> {
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
      script.src = `https://maps.googleapis.com/maps/api/js?key=${environment.googleMapsApiKey}&libraries=places&loading=async&callback=${callbackName}`;
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
}
