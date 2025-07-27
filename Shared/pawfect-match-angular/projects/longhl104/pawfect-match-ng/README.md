# PawfectMatch Angular Library

A comprehensive Angular library providing reusable components, services, and utilities for the PawfectMatch application suite.

## Features

### 🎨 **Custom Icon Component**
- Flexible, expandable icon system with 16+ built-in icons
- Support for custom sizing, colors, and animations
- Easy icon addition through centralized service
- Built with Angular signals for optimal performance

### 🍞 **Toast Service**
- Centralized notification system
- Multiple toast types and customization options

### 🌐 **Google Maps Service**
- Google Maps integration utilities

### ⚡ **Error Handling**
- Global error handling and reporting services

## Installation

```bash
npm install @longhl104/pawfect-match-ng
```

## Quick Start

### Using the Custom Icon Component

```typescript
import { CustomIconComponent } from '@longhl104/pawfect-match-ng';

@Component({
  standalone: true,
  imports: [CustomIconComponent],
  template: `
    <pm-custom-icon name="heart" width="24" color="red"></pm-custom-icon>
    <pm-custom-icon name="star" width="32" color="gold" cssClass="pulse"></pm-custom-icon>
  `
})
export class MyComponent {}
```

### Adding Custom Icons

```typescript
import { IconService, IconDefinition } from '@longhl104/pawfect-match-ng';

@Component({...})
export class MyComponent {
  constructor(private iconService: IconService) {}

  ngOnInit() {
    const customIcon: IconDefinition = {
      name: 'my-icon',
      content: `<path d="M12 2l3.09 6.26L22 9.27..."/>`,
      viewBox: '0 0 24 24'
    };
    
    this.iconService.addIcon(customIcon);
  }
}
```

## Built-in Icons

The library includes these ready-to-use icons:
- `heart`, `heart-filled`
- `star`, `star-filled`
- `paw`, `dog`, `cat`
- `user`, `home`, `search`
- `plus`, `minus`, `check`, `x`
- `arrow-right`, `arrow-left`
- `menu`, `close`

## Components

### Custom Icon Component (`pm-custom-icon`)

| Input | Type | Default | Description |
|-------|------|---------|-------------|
| `name` | `string` | `''` | Icon name |
| `width` | `number \| string` | `24` | Icon size (square) |
| `color` | `string` | `'currentColor'` | Icon color |
| `cssClass` | `string` | `''` | Additional CSS classes |

### Icon Demo Component (`pm-icon-demo`)

A showcase component displaying all available icons and usage examples.

## Services

### IconService

Centralized icon management with methods for adding, removing, and querying icons.

### ToastService

Global notification system for displaying user messages.

### ErrorHandlingService

Centralized error handling and reporting.

## Development

### Building the Library

```bash
ng build pawfect-match-ng
```

### Running Tests

```bash
ng test pawfect-match-ng
```

### Publishing

1. Build the library:
   ```bash
   ng build pawfect-match-ng
   ```

2. Navigate to dist and publish:
   ```bash
   cd dist/pawfect-match-ng
   npm publish
   ```

## Documentation

For detailed component documentation, see the individual README files in each component directory.

## License

This library is part of the PawfectMatch application suite.

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
