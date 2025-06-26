# Global Error Handler Usage Guide

The global error handler has been successfully implemented and configured. Here's how to use it:

## What's Configured

1. **Global Error Handler**: Catches all unhandled JavaScript errors
2. **HTTP Error Interceptor**: Automatically handles HTTP errors from API calls
3. **Injectable Error Handling Service**: Provides error handling methods for components and services

## Usage Options

### Option 1: Injectable Service (Recommended)

For components and services, inject the `ErrorHandlingService`:

```typescript
import { ErrorHandlingService } from 'shared/services/error-handling.service';

@Component({...})
export class MyComponent {
  constructor(private errorHandlingService: ErrorHandlingService) {}

  // Method 1: Basic error handling
  handleSomeError() {
    try {
      // risky operation
    } catch (error) {
      this.errorHandlingService.handleError(error, {
        component: 'MyComponent',
        action: 'handleSomeError'
      });
    }
  }

  // Method 2: Auto-component context
  handleAnotherError() {
    try {
      // risky operation
    } catch (error) {
      this.errorHandlingService.handleErrorWithComponent(error, this, 'handleAnotherError');
    }
  }

  // Method 3: Async operation wrapper
  async saveData() {
    await this.errorHandlingService.handleAsync(
      () => this.apiService.saveData(this.data),
      { component: 'MyComponent', action: 'saveData' }
    );
  }
}
```

### Option 2: Utility Functions (Advanced)

If you need to use error handling outside of Angular components:

```typescript
import { handleError } from 'shared/services/global-error-handler.service';

// Manual error handling
try {
  // some operation
} catch (error) {
  handleError(error, {
    component: 'SomeUtility',
    action: 'doSomething'
  });
}
```

**Note**: The utility functions require the global injector to be set in `main.ts` (already configured).

### Option 3: Automatic HTTP Error Handling

HTTP errors are automatically handled by the interceptor. No additional code needed:

```typescript
// This will automatically show appropriate error toasts
this.http.get('/api/data').subscribe({
  next: (data) => {
    // handle success
  }
  // No need for error handling - the interceptor handles it
});
```

## Error Types and Messages

The system automatically categorizes errors and shows appropriate messages:

- **Network Errors**: "Network connection issue. Please check your internet connection."
- **Authentication Errors (401/403)**: "Authentication failed. Please log in again."
- **Validation Errors (400/422)**: "Please check your input and try again."
- **Server Errors (500+)**: "Server error occurred. Please try again later."
- **Chunk Load Errors**: "Application update detected. Please refresh the page."

## Features

- **Spam Protection**: Prevents showing the same error multiple times
- **Context Awareness**: Includes component and action information in logs
- **User-Friendly Messages**: Converts technical errors to readable messages
- **Development Logging**: Detailed error logs in development mode
- **Toast Integration**: Seamlessly shows errors via the toast service

## Testing

You can test the error handler using the demo component:

```typescript
// Add this to a route for testing
import { ErrorDemo } from './components/error-demo/error-demo';
```

## Already Implemented

- ✅ Global error handler registered in `app.config.ts`
- ✅ HTTP interceptor for automatic API error handling
- ✅ Injectable service for manual error handling
- ✅ Updated registration component to use the new system
- ✅ Spam protection and error deduplication
- ✅ Context-aware error logging

The system is ready to use! HTTP errors will be automatically handled, and you can use the `ErrorHandlingService` for manual error handling in your components.
