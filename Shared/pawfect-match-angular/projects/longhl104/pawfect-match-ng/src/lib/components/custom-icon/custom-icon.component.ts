import { Component, Input, OnInit, OnChanges, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconService, IconDefinition } from '../../services/icon.service';

@Component({
  selector: 'pm-custom-icon',
  standalone: true,
  imports: [CommonModule],
  template: `
    <svg
      [attr.width]="width"
      [attr.height]="width"
      [attr.viewBox]="currentIcon()?.viewBox || '0 0 24 24'"
      [attr.fill]="color"
      [attr.stroke]="color"
      class="custom-icon"
      [class]="cssClass"
      [innerHTML]="currentIcon()?.content || ''"
    >
    </svg>
  `,
  styleUrls: ['./custom-icon.component.scss']
})
export class CustomIconComponent implements OnInit, OnChanges {
  @Input() name = '';
  @Input() width: number | string = 24;
  @Input() color = 'currentColor';
  @Input() cssClass = '';

  private iconService = inject(IconService);

  // Signal for reactive icon lookup
  private iconName = signal<string>('');

  // Computed signal for current icon
  currentIcon = computed(() => {
    const name = this.iconName();
    return this.iconService.getIcon(name);
  });

  ngOnInit() {
    this.iconName.set(this.name);
  }

  ngOnChanges() {
    this.iconName.set(this.name);
  }

  /**
   * Method to add new icon definitions at runtime
   * Useful for dynamic icon loading or plugin systems
   */
  addIcon(icon: IconDefinition) {
    this.iconService.addIcon(icon);
    // Trigger reactivity if current icon was updated
    if (this.iconName() === icon.name) {
      this.iconName.set(icon.name);
    }
  }

  /**
   * Method to get all available icon names
   */
  getAvailableIcons(): string[] {
    return this.iconService.getIconNames();
  }
}
