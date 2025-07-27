# Using Custom Icon Component from PawfectMatch Library

This example shows how to use the custom icon component from the `@longhl104/pawfect-match-ng` library in your components.

## Import and Usage

```typescript
// Example component using the custom icon from the library
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CustomIconComponent, IconService, IconDefinition } from '@longhl104/pawfect-match-ng';

@Component({
  selector: 'app-example',
  standalone: true,
  imports: [CommonModule, CustomIconComponent],
  template: `
    <div class="example-container">
      <h2>Using Custom Icons from Library</h2>
      
      <!-- Basic usage -->
      <div class="icon-section">
        <h3>Basic Icons</h3>
        <pm-custom-icon name="heart" width="24" color="red"></pm-custom-icon>
        <pm-custom-icon name="star" width="32" color="gold"></pm-custom-icon>
        <pm-custom-icon name="paw" width="40" color="brown"></pm-custom-icon>
      </div>

      <!-- Animated icons -->
      <div class="icon-section">
        <h3>Animated Icons</h3>
        <pm-custom-icon name="heart-filled" width="32" color="red" cssClass="pulse"></pm-custom-icon>
        <pm-custom-icon name="star-filled" width="32" color="gold" cssClass="spin"></pm-custom-icon>
        <pm-custom-icon name="paw" width="32" color="blue" cssClass="bounce"></pm-custom-icon>
      </div>

      <!-- Custom added icon -->
      <div class="icon-section">
        <h3>Custom Icon</h3>
        <pm-custom-icon name="adoption" width="36" color="green"></pm-custom-icon>
        <span>Custom adoption icon added dynamically!</span>
      </div>

      <!-- Size variants -->
      <div class="icon-section">
        <h3>Different Sizes</h3>
        <pm-custom-icon name="dog" width="16"></pm-custom-icon>
        <pm-custom-icon name="dog" width="24"></pm-custom-icon>
        <pm-custom-icon name="dog" width="32"></pm-custom-icon>
        <pm-custom-icon name="dog" width="48"></pm-custom-icon>
      </div>
    </div>
  `,
  styles: [`
    .example-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;
    }

    .icon-section {
      margin-bottom: 2rem;
      padding: 1rem;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      background: #fafafa;
    }

    .icon-section h3 {
      margin-top: 0;
      color: #333;
      border-bottom: 2px solid #ddd;
      padding-bottom: 0.5rem;
    }

    .icon-section pm-custom-icon {
      margin-right: 1rem;
      margin-bottom: 0.5rem;
    }

    h2 {
      text-align: center;
      color: #333;
      margin-bottom: 2rem;
    }
  `]
})
export class ExampleComponent implements OnInit {
  private iconService = inject(IconService);

  ngOnInit() {
    // Add a custom icon specific to our app
    this.addCustomIcons();
  }

  private addCustomIcons() {
    const customIcons: IconDefinition[] = [
      {
        name: 'adoption',
        content: `
          <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2z"/>
          <path d="M8 14s1.5 2 4 2 4-2 4-2"/>
          <circle cx="9" cy="9" r="1"/>
          <circle cx="15" cy="9" r="1"/>
          <path d="M12 17.5c2.33 0 4.31-1.46 5.11-3.5H6.89c.8 2.04 2.78 3.5 5.11 3.5z"/>
        `,
        viewBox: '0 0 24 24'
      },
      {
        name: 'shelter',
        content: `
          <path d="M12 3l8 8v10H4V11l8-8z"/>
          <path d="M12 3v18"/>
          <path d="M4 11h16"/>
          <circle cx="8" cy="15" r="1"/>
          <circle cx="16" cy="15" r="1"/>
        `,
        viewBox: '0 0 24 24'
      }
    ];

    this.iconService.addIcons(customIcons);
  }
}
```

## In Template Usage

```html
<!-- Replace PrimeNG icons with custom icons -->

<!-- Before (PrimeNG) -->
<p-button icon="pi pi-heart" label="Favorite"></p-button>

<!-- After (Custom Icon Library) -->
<p-button label="Favorite">
  <pm-custom-icon name="heart-filled" width="16" color="currentColor"></pm-custom-icon>
</p-button>

<!-- Or in standalone usage -->
<button class="custom-btn">
  <pm-custom-icon name="heart" width="20" color="red" cssClass="hoverable"></pm-custom-icon>
  Add to Favorites
</button>
```

## Integration in Landing Component

```typescript
// In your landing.component.ts
import { CustomIconComponent } from '@longhl104/pawfect-match-ng';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [
    // ... other imports
    CustomIconComponent
  ],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.scss']
})
export class LandingComponent {
  // ... existing code
}
```

```html
<!-- In your landing.component.html -->
<!-- Replace the PrimeNG heart icon in the CTA section -->
<section class="primary-cta">
  <div class="container">
    <div class="cta-content">
      <!-- Before -->
      <!-- <i class="pi pi-heart-fill heart-icon pulse-animation"></i> -->
      
      <!-- After -->
      <pm-custom-icon 
        name="heart-filled" 
        width="64" 
        color="white" 
        cssClass="heart-icon pulse">
      </pm-custom-icon>
      
      <h2 class="cta-headline">Ready to meet your perfect match?</h2>
      <!-- ... rest of the content -->
    </div>
  </div>
</section>
```

## Available Icons from Library

- `heart`, `heart-filled`
- `star`, `star-filled`
- `paw`, `dog`, `cat`
- `user`, `home`, `search`
- `plus`, `minus`, `check`, `x`
- `arrow-right`, `arrow-left`
- `menu`, `close`

Plus any custom icons you add via the `IconService`!
