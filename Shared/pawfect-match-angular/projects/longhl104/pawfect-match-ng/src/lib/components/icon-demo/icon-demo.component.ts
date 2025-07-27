import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CustomIconComponent } from '../custom-icon/custom-icon.component';
import { IconService, IconDefinition } from '../../services/icon.service';

@Component({
  selector: 'pm-icon-demo',
  standalone: true,
  imports: [CommonModule, CustomIconComponent],
  template: `
    <div class="icon-demo">
      <h2>Custom Icon Component Demo</h2>

      <section class="demo-section">
        <h3>Basic Usage</h3>
        <div class="icon-row">
          <pm-custom-icon name="heart" width="24" color="red"></pm-custom-icon>
          <pm-custom-icon name="star" width="32" color="gold"></pm-custom-icon>
          <pm-custom-icon name="paw" width="40" color="brown"></pm-custom-icon>
        </div>
      </section>

      <section class="demo-section">
        <h3>Different Sizes</h3>
        <div class="icon-row">
          <pm-custom-icon name="dog" width="16" cssClass="icon-xs"></pm-custom-icon>
          <pm-custom-icon name="cat" width="24" cssClass="icon-md"></pm-custom-icon>
          <pm-custom-icon name="user" width="32" cssClass="icon-lg"></pm-custom-icon>
          <pm-custom-icon name="home" width="48" cssClass="icon-xl"></pm-custom-icon>
        </div>
      </section>

      <section class="demo-section">
        <h3>With Animations</h3>
        <div class="icon-row">
          <pm-custom-icon name="heart-filled" width="32" color="red" cssClass="pulse"></pm-custom-icon>
          <pm-custom-icon name="star-filled" width="32" color="gold" cssClass="spin"></pm-custom-icon>
          <pm-custom-icon name="paw" width="32" color="blue" cssClass="bounce"></pm-custom-icon>
        </div>
      </section>

      <section class="demo-section">
        <h3>Hoverable Icons</h3>
        <div class="icon-row">
          <pm-custom-icon name="search" width="28" cssClass="hoverable"></pm-custom-icon>
          <pm-custom-icon name="plus" width="28" cssClass="hoverable"></pm-custom-icon>
          <pm-custom-icon name="check" width="28" cssClass="hoverable"></pm-custom-icon>
        </div>
      </section>

      <section class="demo-section">
        <h3>All Available Icons</h3>
        <div class="icon-grid">
          @for (iconName of availableIcons; track iconName) {
            <div class="icon-item">
              <pm-custom-icon [name]="iconName" width="24"></pm-custom-icon>
              <span class="icon-name">{{ iconName }}</span>
            </div>
          }
        </div>
      </section>

      <section class="demo-section">
        <h3>Custom Added Icon</h3>
        <div class="icon-row">
          <pm-custom-icon name="custom-heart" width="32" color="purple"></pm-custom-icon>
          <span>This icon was added dynamically!</span>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .icon-demo {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;
    }

    .demo-section {
      margin-bottom: 2rem;
      padding: 1rem;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
    }

    .demo-section h3 {
      margin-top: 0;
      color: #333;
    }

    .icon-row {
      display: flex;
      align-items: center;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .icon-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
      gap: 1rem;
      margin-top: 1rem;
    }

    .icon-item {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 0.5rem;
      border: 1px solid #f0f0f0;
      border-radius: 4px;
      text-align: center;
    }

    .icon-name {
      font-size: 0.8rem;
      margin-top: 0.5rem;
      color: #666;
    }

    h2 {
      text-align: center;
      color: #333;
      margin-bottom: 2rem;
    }
  `]
})
export class IconDemoComponent implements OnInit {
  private iconService = inject(IconService);
  availableIcons: string[] = [];

  ngOnInit() {
    this.availableIcons = this.iconService.getIconNames();

    // Add a custom icon to demonstrate expandability
    this.addCustomIcon();
  }

  private addCustomIcon() {
    const customIcon: IconDefinition = {
      name: 'custom-heart',
      content: `<path fill="currentColor" d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>`,
      viewBox: '0 0 24 24'
    };

    this.iconService.addIcon(customIcon);
  }
}
