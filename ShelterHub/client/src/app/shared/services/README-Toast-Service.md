# Toast Service Documentation

## Overview

The Toast Service provides a clean and user-friendly way to display temporary notifications to users in an overlay format. It supports different types of messages (success, error, warning, info) with customizable duration and styling.

## Features

- ✅ Multiple toast types: success, error, warning, info
- ✅ Auto-dismiss with customizable duration
- ✅ Manual close button
- ✅ Responsive design
- ✅ Smooth animations
- ✅ Queue management (multiple toasts)
- ✅ Accessibility support

## Installation

The toast service is already set up in your application. The `ToastContainerComponent` is included in the main app component, so toasts will display globally across your application.

## Usage

### Basic Usage

```typescript
import { ToastService } from 'shared/services/toast.service';

constructor(private toastService: ToastService) {}

// Show different types of toasts
this.toastService.success('Operation completed successfully!');
this.toastService.error('Something went wrong!');
this.toastService.warning('Please be careful!');
this.toastService.info('Here is some information.');
```

### Advanced Usage

```typescript
// Custom duration (in milliseconds)
this.toastService.success("This will show for 10 seconds", 10000);

// Non-closable toast
this.toastService.error("Critical error", 0, false);

// Clear all toasts
this.toastService.clearAll();

// Remove specific toast by ID
const toastId = this.toastService.success("Test message").id;
this.toastService.removeToast(toastId);
```

## Toast Types

### Success Toast

- **Color**: Green
- **Icon**: Checkmark
- **Default Duration**: 5 seconds
- **Use Case**: Successful operations, confirmations

### Error Toast

- **Color**: Red
- **Icon**: X mark
- **Default Duration**: 8 seconds
- **Use Case**: Errors, failures, validation issues

### Warning Toast

- **Color**: Orange/Yellow
- **Icon**: Triangle with exclamation
- **Default Duration**: 5 seconds
- **Use Case**: Warnings, cautionary messages

### Info Toast

- **Color**: Blue
- **Icon**: Information symbol
- **Default Duration**: 5 seconds
- **Use Case**: General information, tips, updates

## Styling

The toast container is positioned fixed at the top-right of the screen with the following features:

- Responsive design (full width on mobile)
- High z-index (10000) to appear above other content
- Smooth slide-in animations
- Hover effects for better interactivity

## Configuration Options

### Duration

- Default duration: 5 seconds (5000ms)
- Error messages: 8 seconds by default
- Set to 0 for persistent toasts
- Custom duration can be passed for any toast type

### Closable

- Default: `true` (shows close button)
- Set to `false` to hide close button
- Users can still dismiss by waiting for auto-timeout

## Examples in Your Application

### Registration Success

```typescript
await this.adoptersService.register(finalData);
this.toastService.success("Registration successful! Welcome to PawfectMatch!");
this.router.navigate(["/auth/login"]);
```

### Form Validation Error

```typescript
if (!this.registrationForm.valid) {
  this.toastService.error("Please fill in all required fields correctly.");
  return;
}
```

### API Error Handling

```typescript
try {
  await this.apiCall();
  this.toastService.success("Data saved successfully!");
} catch (error) {
  this.toastService.error("Failed to save data. Please try again.");
}
```

## Demo Component

A demo component is available at `shared/components/toast-demo/toast-demo.component.ts` to test all toast functionality.

## Accessibility

- ARIA labels on close buttons
- Proper color contrast ratios
- Keyboard accessible close buttons
- Screen reader compatible

## Browser Compatibility

- Modern browsers (Chrome, Firefox, Safari, Edge)
- Responsive design for mobile devices
- CSS animations with fallbacks
