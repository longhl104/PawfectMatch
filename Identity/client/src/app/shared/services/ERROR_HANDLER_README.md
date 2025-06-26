# Global Error Handler Documentation

## Overview

The Global Error Handler provides comprehensive error handling throughout the Angular application using the Toast Service to display user-friendly error messages. It automatically captures unhandled errors, HTTP errors, and provides utilities for manual error handling.

## Features

- **Automatic Error Capture**: Catches all unhandled errors in the application
- **HTTP Error Interception**: Automatically handles HTTP errors with appropriate user messages
- **Error Deduplication**: Prevents spam by limiting repeated error messages
- **Context-Aware Logging**: Adds component and action context to errors
- **User-Friendly Messages**: Converts technical errors into readable messages
- **Toast Integration**: Seamlessly integrates with the Toast Service
- **External Logging Support**: Ready for integration with external logging services

## Setup

The global error handler is automatically configured in `app.config.ts`:

```typescript
import { GlobalErrorHandler } from './shared/services/global-error-handler.service';
import { errorInterceptor } from './shared/interceptors/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    // ... other providers
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    provideHttpClient(withFetch(), withInterceptors([errorInterceptor])),
  ],
};
```

## Error Types Handled

### 1. Network Errors
- Connection timeouts
- Network connectivity issues
- Server unreachable (status 0)

**User Message**: "Network connection issue. Please check your internet connection."

### 2. Authentication Errors (401, 403)
- Unauthorized access
- Forbidden resources

**User Message**: "Authentication failed. Please log in again."

### 3. Validation Errors (400, 422)
- Bad request data
- Validation failures

**User Message**: "Please check your input and try again."

### 4. Server Errors (500+)
- Internal server errors
- Service unavailable

**User Message**: "Server error occurred. Please try again later."

### 5. Chunk Load Errors
- Application update issues
- Bundle loading failures

**User Message**: "Application update detected. Please refresh the page."

## Usage Examples

### 1. Automatic Error Handling with Decorator

Use the `@HandleErrors` decorator for automatic error handling:

```typescript
import { HandleErrors } from 'shared/services/global-error-handler.service';

@Component({...})
export class MyComponent {

  @HandleErrors({ component: 'MyComponent' })
  async saveData(): Promise<void> {
    // Any error thrown here will be automatically handled
    await this.dataService.save(this.formData);
  }
}
```

### 2. Manual Error Handling

Use the `handleError` function for manual error handling with context:

```typescript
import { handleError } from 'shared/services/global-error-handler.service';

export class MyService {

  processData(data: any): void {
    try {
      // Some risky operation
      this.riskyOperation(data);
    } catch (error) {
      handleError(error, {
        component: 'MyService',
        action: 'processData',
        userId: this.currentUserId
      });
    }
  }
}
```

### 3. HTTP Error Handling

HTTP errors are automatically handled by the error interceptor, but you can add additional context:

```typescript
export class ApiService {

  async createUser(userData: any): Promise<User> {
    try {
      return await firstValueFrom(this.http.post<User>('/api/users', userData));
    } catch (error) {
      // HTTP interceptor will handle the error, but we can add context
      handleError(error, {
        component: 'ApiService',
        action: 'createUser',
        userId: userData.email
      });
      throw error; // Re-throw if needed
    }
  }
}
```

### 4. Observable Error Handling

For RxJS observables, errors are handled through the HTTP interceptor, but you can add additional handling:

```typescript
import { catchError } from 'rxjs/operators';

export class DataService {

  getData(): Observable<Data[]> {
    return this.http.get<Data[]>('/api/data').pipe(
      catchError(error => {
        handleError(error, {
          component: 'DataService',
          action: 'getData'
        });
        return throwError(() => error);
      })
    );
  }
}
```

## Error Context

You can provide additional context when handling errors:

```typescript
interface ErrorContext {
  component?: string;    // Component/service name
  action?: string;       // Method/function name
  userId?: string;       // Current user identifier
  timestamp?: Date;      // When the error occurred
  userAgent?: string;    // Browser information
  url?: string;          // Current page URL
}
```

## Configuration

### Error Suppression

The error handler includes built-in spam protection:

- **Max Same Error Count**: 3 occurrences of the same error
- **Cache Window**: 60 seconds (1 minute)

Modify these values in `global-error-handler.service.ts`:

```typescript
private readonly MAX_SAME_ERROR_COUNT = 3;
private readonly ERROR_CACHE_WINDOW = 60000; // 1 minute
```

### Toast Duration

Error toast duration varies by error type:

- **Network/Server Errors**: 10 seconds
- **Validation Errors**: 6 seconds
- **Chunk Load Errors**: 15 seconds
- **Default**: 8 seconds

### External Logging

To integrate with external logging services (like Sentry), modify the `logToExternalService` method:

```typescript
private logToExternalService(originalError: any, errorInfo: any, context?: ErrorContext): void {
  // Example: Send to Sentry
  Sentry.captureException(originalError, {
    contexts: {
      error_info: errorInfo,
      custom_context: context
    }
  });

  // Example: Send to your API
  this.http.post('/api/logs/client-errors', {
    error: originalError,
    context: context,
    timestamp: new Date()
  }).subscribe();
}
```

## Testing

To test the error handler:

```typescript
// In component for testing
testError(): void {
  throw new Error('Test error message');
}

// HTTP error test
testHttpError(): void {
  this.http.get('/api/nonexistent').subscribe();
}

// Manual error with context
testManualError(): void {
  handleError(new Error('Manual test error'), {
    component: 'TestComponent',
    action: 'testManualError',
    userId: 'test-user'
  });
}
```

## Best Practices

1. **Use Decorators for Components**: Use `@HandleErrors` on component methods that might fail
2. **Provide Context**: Always include component and action context when manually handling errors
3. **Don't Over-Handle**: Let the global handler do its job; only add manual handling when you need specific context
4. **Re-throw When Needed**: If calling code needs to know about the error, re-throw after handling
5. **User-Friendly Messages**: The handler converts technical errors to user-friendly messages automatically

## Migration

If you have existing error handling:

### Before
```typescript
try {
  await this.service.save(data);
} catch (error) {
  console.error(error);
  this.toastService.error('Save failed');
}
```

### After
```typescript
@HandleErrors({ component: 'MyComponent' })
async saveData(): Promise<void> {
  await this.service.save(data);
  // Success handling only - errors handled automatically
}
```

## Troubleshooting

### Common Issues

1. **Error Handler Not Working**: Ensure it's registered in `app.config.ts`
2. **HTTP Errors Not Intercepted**: Check that `errorInterceptor` is registered
3. **Too Many Toasts**: The handler includes spam protection; check if you're manually handling errors that are already handled
4. **Context Not Appearing**: Make sure you're passing the context object correctly

### Debug Mode

In development, detailed error logs are shown in the console. Look for the "🚨 Global Error Handler - Detailed Log" group.

## File Structure

```
src/app/shared/
├── services/
│   ├── global-error-handler.service.ts     # Main error handler
│   ├── error-handling-example.service.ts   # Usage examples
│   └── toast.service.ts                    # Toast service
├── interceptors/
│   └── error.interceptor.ts                # HTTP error interceptor
└── README.md                               # This documentation
```
