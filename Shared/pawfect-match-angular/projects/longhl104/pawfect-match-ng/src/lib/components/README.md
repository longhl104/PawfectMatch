# ToastContainerComponent

A standalone Angular component for displaying toast notifications in your applications.

## Features

- ✅ **Multiple toast types**: success, error, warning, info
- ✅ **Auto-dismissible**: Configurable duration for automatic dismissal
- ✅ **Manually dismissible**: Optional close button
- ✅ **Responsive design**: Adapts to mobile screens
- ✅ **Smooth animations**: Slide-in animation with hover effects
- ✅ **Accessibility**: Proper ARIA labels and keyboard support
- ✅ **Modern styling**: Clean, professional appearance with icons

## Usage

### 1. Import in your component or module

```typescript
import { ToastContainerComponent } from '@longhl104/pawfect-match-ng';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [ToastContainerComponent],
  template: `
    <div class="app-content">
      <!-- Your app content -->
    </div>
    <pm-toast-container></pm-toast-container>
  `
})
export class AppComponent {}
```

### 2. Use the ToastService to show notifications

```typescript
import { ToastService } from '@longhl104/pawfect-match-ng';

@Component({...})
export class MyComponent {
  private toastService = inject(ToastService);

  showSuccess() {
    this.toastService.success('Operation completed successfully!');
  }

  showError() {
    this.toastService.error('Something went wrong!', 10000); // 10 seconds
  }

  showWarning() {
    this.toastService.warning('Please check your input');
  }

  showInfo() {
    this.toastService.info('Here\'s some helpful information');
  }
}
```

## Component API

### Selector
- `pm-toast-container`

### Methods

| Method | Description | Parameters |
|--------|-------------|------------|
| `closeToast(id: string)` | Manually close a specific toast | `id`: Toast identifier |
| `trackByToastId(index: number, toast: Toast)` | TrackBy function for ngFor | `index`, `toast` |

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `toasts$` | `Observable<Toast[]>` | Observable stream of current toasts |

## Toast Interface

```typescript
interface Toast {
  id: string;
  message: string;
  type: 'success' | 'error' | 'warning' | 'info';
  duration?: number;    // Auto-dismiss duration in ms
  closable?: boolean;   // Show close button
}
```

## Styling

The component comes with built-in responsive styling. You can customize the appearance by overriding CSS variables or classes:

### CSS Classes
- `.toast-container` - Main container
- `.toast` - Individual toast wrapper
- `.toast-success`, `.toast-error`, `.toast-warning`, `.toast-info` - Toast type classes
- `.toast-icon` - Icon container
- `.toast-content` - Message content area
- `.toast-message` - Message text
- `.toast-close` - Close button

### Positioning
By default, toasts appear in the top-right corner. You can customize positioning by overriding the `.toast-container` styles:

```css
/* Bottom-left positioning */
.toast-container {
  top: auto;
  bottom: 20px;
  right: auto;
  left: 20px;
}
```

## Accessibility

The component includes:
- Proper ARIA labels for close buttons
- Semantic HTML structure
- Keyboard navigation support
- Screen reader friendly content

## Best Practices

1. **Placement**: Add the `<pm-toast-container>` once in your root component
2. **Duration**: Use longer durations for error messages (8-10s) and shorter for success (3-5s)
3. **Message content**: Keep messages concise and actionable
4. **Frequency**: Avoid showing too many toasts simultaneously to prevent spam

## Examples

### Basic Usage
```typescript
// Show a simple success message
this.toastService.success('Settings saved!');

// Show an error with custom duration and closable
this.toastService.error('Failed to save settings', 8000, true);
```

### With Error Handling
```typescript
async saveSettings() {
  try {
    await this.settingsService.save(this.settings);
    this.toastService.success('Settings saved successfully!');
  } catch (error) {
    this.toastService.error('Failed to save settings. Please try again.');
  }
}
```

### Clearing All Toasts
```typescript
// Clear all currently displayed toasts
this.toastService.clearAll();
```
