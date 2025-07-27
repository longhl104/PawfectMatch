# Custom Icon Component - PawfectMatch Library

A flexible, expandable Angular icon component that supports dynamic icon names, sizing, and colors.

## Features

- ✅ **Flexible Input Support**: Name, width/height, and color inputs
- ✅ **Easy Expandability**: Simple icon addition through service
- ✅ **Reactive Design**: Built with Angular signals for optimal performance
- ✅ **TypeScript Support**: Full type safety with IconDefinition interface
- ✅ **CSS Animations**: Built-in animation classes (spin, pulse, bounce)
- ✅ **Accessibility**: Proper ARIA attributes and keyboard navigation
- ✅ **Customizable Styling**: CSS classes and inline styles support

## Installation

This component is part of the `@longhl104/pawfect-match-ng` library.

```bash
npm install @longhl104/pawfect-match-ng
```

## Basic Usage

```html
<!-- Basic icon -->
<pm-custom-icon name="heart" width="24" color="red"></pm-custom-icon>

<!-- With CSS classes -->
<pm-custom-icon 
  name="star" 
  width="32" 
  color="gold" 
  cssClass="pulse hoverable">
</pm-custom-icon>

<!-- Different sizes -->
<pm-custom-icon name="paw" width="16"></pm-custom-icon>
<pm-custom-icon name="paw" width="24"></pm-custom-icon>
<pm-custom-icon name="paw" width="48"></pm-custom-icon>
```

## Component Inputs

| Input | Type | Default | Description |
|-------|------|---------|-------------|
| `name` | `string` | `''` | Name of the icon to display |
| `width` | `number \| string` | `24` | Width and height of the icon (assumes square) |
| `color` | `string` | `'currentColor'` | Color of the icon (CSS color value) |
| `cssClass` | `string` | `''` | Additional CSS classes to apply |

## Adding New Icons

### Method 1: Through IconService (Recommended)

```typescript
import { IconService, IconDefinition } from '@longhl104/pawfect-match-ng';

@Component({...})
export class MyComponent {
  constructor(private iconService: IconService) {}

  ngOnInit() {
    // Add a single icon
    const newIcon: IconDefinition = {
      name: 'my-custom-icon',
      content: `<path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>`,
      viewBox: '0 0 24 24'
    };
    
    this.iconService.addIcon(newIcon);

    // Add multiple icons
    const icons: IconDefinition[] = [
      { name: 'icon1', content: '...' },
      { name: 'icon2', content: '...' }
    ];
    
    this.iconService.addIcons(icons);
  }
}
```

### Method 2: Through Component Instance

```typescript
@Component({
  template: `
    <pm-custom-icon #iconComponent name="heart" width="24"></pm-custom-icon>
  `
})
export class MyComponent {
  @ViewChild('iconComponent') iconComponent!: CustomIconComponent;

  ngAfterViewInit() {
    this.iconComponent.addIcon({
      name: 'runtime-icon',
      content: `<circle cx="12" cy="12" r="10"/>`,
      viewBox: '0 0 24 24'
    });
  }
}
```

## Built-in Icons

The component comes with these pre-built icons:

- `heart` / `heart-filled`
- `star` / `star-filled`
- `paw`
- `dog` / `cat`
- `user`
- `home`
- `search`
- `plus` / `minus`
- `check` / `x`
- `arrow-right` / `arrow-left`
- `menu` / `close`

## CSS Classes

### Size Classes

- `icon-xs` (12px)
- `icon-sm` (16px)
- `icon-md` (24px)
- `icon-lg` (32px)
- `icon-xl` (48px)

### Animation Classes

- `pulse` - Pulsing animation
- `spin` - Rotating animation
- `bounce` - Bouncing animation
- `hoverable` - Scale on hover

### Custom Styling

```scss
// Custom icon styles
.my-custom-icon {
  color: #ff6b6b;
  transition: all 0.3s ease;
  
  &:hover {
    color: #ff5252;
    transform: scale(1.1);
  }
}
```

## IconDefinition Interface

```typescript
export interface IconDefinition {
  name: string;          // Unique identifier for the icon
  content: string;       // SVG path/content (without <svg> wrapper)
  viewBox?: string;      // SVG viewBox (defaults to "0 0 24 24")
}
```

## Service Methods

The `IconService` provides these methods:

```typescript
// Get all icons
getIcons(): Signal<IconDefinition[]>

// Get specific icon
getIcon(name: string): IconDefinition | undefined

// Add single icon
addIcon(icon: IconDefinition): void

// Add multiple icons
addIcons(icons: IconDefinition[]): void

// Remove icon
removeIcon(name: string): void

// Get all icon names
getIconNames(): string[]

// Check if icon exists
hasIcon(name: string): boolean
```

## Examples

### Pet Care App Icons

```typescript
const petIcons: IconDefinition[] = [
  {
    name: 'food-bowl',
    content: `<circle cx="12" cy="16" r="6"/><path d="M12 10v6"/>`,
    viewBox: '0 0 24 24'
  },
  {
    name: 'vet',
    content: `<path d="M12 2l2 7h7l-5.5 4.5L17 21l-5-4-5 4 1.5-7.5L3 9h7l2-7z"/>`,
    viewBox: '0 0 24 24'
  }
];

this.iconService.addIcons(petIcons);
```

### Usage in Templates

```html
<!-- Static icons -->
<pm-custom-icon name="food-bowl" width="24" color="#8B4513"></pm-custom-icon>
<pm-custom-icon name="vet" width="28" color="#4CAF50"></pm-custom-icon>

<!-- Dynamic icons -->
<pm-custom-icon 
  [name]="selectedIcon" 
  [width]="iconSize" 
  [color]="iconColor"
  [cssClass]="animationClass">
</pm-custom-icon>

<!-- Loop through icons -->
@for (iconName of iconNames; track iconName) {
  <pm-custom-icon 
    [name]="iconName" 
    width="20" 
    cssClass="hoverable">
  </pm-custom-icon>
}
```

## Demo Component

You can also import the demo component to see all available icons:

```typescript
import { IconDemoComponent } from '@longhl104/pawfect-match-ng';

@Component({
  imports: [IconDemoComponent],
  template: '<pm-icon-demo></pm-icon-demo>'
})
export class MyComponent {}
```

## Best Practices

1. **Consistent Naming**: Use kebab-case for icon names (`heart-filled`, `arrow-right`)
2. **ViewBox Standards**: Stick to `0 0 24 24` for consistency unless specific requirements
3. **Performance**: Add icons during component initialization, not in templates
4. **Accessibility**: Use semantic HTML and ARIA labels when needed
5. **SVG Optimization**: Optimize SVG paths before adding to reduce bundle size

## Integration

### Standalone Components

```typescript
import { CustomIconComponent } from '@longhl104/pawfect-match-ng';

@Component({
  standalone: true,
  imports: [CustomIconComponent],
  template: '<pm-custom-icon name="heart" width="24"></pm-custom-icon>'
})
export class MyComponent {}
```

### NgModules

```typescript
import { CustomIconComponent } from '@longhl104/pawfect-match-ng';

@NgModule({
  imports: [CustomIconComponent],
  // ...
})
export class MyModule {}
```
