import { Injectable, signal } from '@angular/core';
import { IconDefinition, icons } from './icons';

@Injectable({
  providedIn: 'root',
})
export class IconService {
  private icons = signal<IconDefinition[]>(icons);

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
    return this.icons().find((icon) => icon.name === name);
  }

  /**
   * Add a new icon to the registry
   */
  addIcon(icon: IconDefinition): void {
    const currentIcons = this.icons();
    const existingIndex = currentIcons.findIndex((i) => i.name === icon.name);

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
    const iconMap = new Map(currentIcons.map((icon) => [icon.name, icon]));

    // Add or update icons
    icons.forEach((icon) => {
      iconMap.set(icon.name, icon);
    });

    this.icons.set(Array.from(iconMap.values()));
  }

  /**
   * Remove an icon from the registry
   */
  removeIcon(name: string): void {
    const currentIcons = this.icons();
    this.icons.set(currentIcons.filter((icon) => icon.name !== name));
  }

  /**
   * Get all available icon names
   */
  getIconNames(): string[] {
    return this.icons().map((icon) => icon.name);
  }

  /**
   * Check if an icon exists
   */
  hasIcon(name: string): boolean {
    return this.icons().some((icon) => icon.name === name);
  }
}
