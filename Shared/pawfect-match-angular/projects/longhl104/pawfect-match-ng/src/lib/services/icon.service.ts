import { Injectable, signal } from '@angular/core';

export interface IconDefinition {
  name: string;
  content: string;
  viewBox?: string;
}

@Injectable({
  providedIn: 'root'
})
export class IconService {
  private icons = signal<IconDefinition[]>([
    {
      name: 'heart',
      content: `<path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'heart-filled',
      content: `<path fill="currentColor" d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'paw',
      content: `<path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.94-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'dog',
      content: `<path d="M4.5 12.5C4.5 12.78 4.72 13 5 13s.5-.22.5-.5S5.28 12 5 12s-.5.22-.5.5zM9 13c.28 0 .5-.22.5-.5S9.28 12 9 12s-.5.22-.5.5.22.5.5.5zm2.5-9c1.38 0 2.5 1.12 2.5 2.5S12.88 9 11.5 9 9 7.88 9 6.5 10.12 4 11.5 4zm-5 7c-.83 0-1.5.67-1.5 1.5S5.67 14 6.5 14 8 13.33 8 12.5 7.33 11 6.5 11zm11 0c-.83 0-1.5.67-1.5 1.5s.67 1.5 1.5 1.5 1.5-.67 1.5-1.5-.67-1.5-1.5-1.5zM12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8 0-1.12.23-2.18.65-3.15-.01.05-.01.1-.01.15 0 1.38 1.12 2.5 2.5 2.5h.86c.01 0 .01 0 .02 0C8.78 11.74 9.76 12 10.86 12c1.1 0 2.08-.26 2.84-.5.01 0 .01 0 .02 0h.86c1.38 0 2.5-1.12 2.5-2.5 0-.05-.01-.1-.01-.15.42.97.65 2.03.65 3.15 0 4.41-3.59 8-8 8z"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'cat',
      content: `<path d="M4.5 12.5C4.5 12.78 4.72 13 5 13s.5-.22.5-.5S5.28 12 5 12s-.5.22-.5.5zM9 13c.28 0 .5-.22.5-.5S9.28 12 9 12s-.5.22-.5.5.22.5.5.5zm6-7l2-3c.18-.27.54-.34.8-.15.27.18.34.54.15.8L16.5 6h1.25c.41 0 .75.34.75.75v.5c0 .41-.34.75-.75.75H16.5l1.45 2.65c.18.27.11.63-.15.8-.27.18-.63.11-.8-.15L15 7.5V6h-1zm-9 4c-.83 0-1.5.67-1.5 1.5S5.17 14 6 14s1.5-.67 1.5-1.5S6.83 10 6 10zm12 0c-.83 0-1.5.67-1.5 1.5S17.17 14 18 14s1.5-.67 1.5-1.5S18.83 10 18 10zM12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8z"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'star',
      content: `<path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'star-filled',
      content: `<path fill="currentColor" d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'user',
      content: `<path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'home',
      content: `<path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9,22 9,12 15,12 15,22"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'search',
      content: `<circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'plus',
      content: `<line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'minus',
      content: `<line x1="5" y1="12" x2="19" y2="12"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'check',
      content: `<polyline points="20,6 9,17 4,12"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'x',
      content: `<line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'arrow-right',
      content: `<line x1="5" y1="12" x2="19" y2="12"/><polyline points="12,5 19,12 12,19"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'arrow-left',
      content: `<line x1="19" y1="12" x2="5" y2="12"/><polyline points="12,19 5,12 12,5"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'menu',
      content: `<line x1="3" y1="6" x2="21" y2="6"/><line x1="3" y1="12" x2="21" y2="12"/><line x1="3" y1="18" x2="21" y2="18"/>`,
      viewBox: '0 0 24 24'
    },
    {
      name: 'close',
      content: `<line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>`,
      viewBox: '0 0 24 24'
    }
  ]);

  /**
   * Get all available icons as a readonly signal
   */
  getIcons() {
    return this.icons.asReadonly();
  }

  /**
   * Get a specific icon by name
   */
  getIcon(name: string): IconDefinition | undefined {
    return this.icons().find(icon => icon.name === name);
  }

  /**
   * Add a new icon to the registry
   */
  addIcon(icon: IconDefinition): void {
    const currentIcons = this.icons();
    const existingIndex = currentIcons.findIndex(i => i.name === icon.name);

    if (existingIndex >= 0) {
      // Replace existing icon
      const updatedIcons = [...currentIcons];
      updatedIcons[existingIndex] = icon;
      this.icons.set(updatedIcons);
    } else {
      // Add new icon
      this.icons.set([...currentIcons, icon]);
    }
  }

  /**
   * Add multiple icons at once
   */
  addIcons(icons: IconDefinition[]): void {
    const currentIcons = this.icons();
    const iconMap = new Map(currentIcons.map(icon => [icon.name, icon]));

    // Add or update icons
    icons.forEach(icon => {
      iconMap.set(icon.name, icon);
    });

    this.icons.set(Array.from(iconMap.values()));
  }

  /**
   * Remove an icon from the registry
   */
  removeIcon(name: string): void {
    const currentIcons = this.icons();
    this.icons.set(currentIcons.filter(icon => icon.name !== name));
  }

  /**
   * Get all available icon names
   */
  getIconNames(): string[] {
    return this.icons().map(icon => icon.name);
  }

  /**
   * Check if an icon exists
   */
  hasIcon(name: string): boolean {
    return this.icons().some(icon => icon.name === name);
  }
}
