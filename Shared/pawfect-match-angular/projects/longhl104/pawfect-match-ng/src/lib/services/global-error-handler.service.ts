/* eslint-disable @typescript-eslint/no-explicit-any */
import { ErrorHandler, Injectable, inject, Injector } from '@angular/core';
import { ToastService } from './toast.service';

export interface ErrorContext {
  component?: string;
  action?: string;
  userId?: string;
  timestamp?: Date;
  userAgent?: string;
  url?: string;
}

@Injectable({
  providedIn: 'root',
})
export class GlobalErrorHandler implements ErrorHandler {
  private toastService = inject(ToastService);

  // Track error frequency to prevent spam
  private errorCache = new Map<string, { count: number; lastSeen: number }>();
  private readonly MAX_SAME_ERROR_COUNT = 3;
  private readonly ERROR_CACHE_WINDOW = 60000; // 1 minute

  handleError(error: any, context?: ErrorContext): void {
    // Log the error to console for debugging
    console.error('Global Error Handler:', error, context);

    // Extract meaningful error information
    const errorInfo = this.extractErrorInfo(error);

    // Check if we should suppress this error (prevent spam)
    if (this.shouldSuppressError(errorInfo.key)) {
      return;
    }

    // Update error cache
    this.updateErrorCache(errorInfo.key);

    // Show user-friendly toast message
    this.showUserFriendlyError(errorInfo);

    // Log to external service if needed (implement as needed)
    this.logToExternalService(error, errorInfo, context);
  }

  private extractErrorInfo(error: any): {
    message: string;
    type: string;
    key: string;
  } {
    let message = 'An unexpected error occurred';
    let type = 'UnknownError';
    let originalMessage = '';

    if (error instanceof Error) {
      originalMessage = error.message;
      type = error.name ?? 'Error';
    } else if (typeof error === 'string') {
      originalMessage = error;
      type = 'StringError';
    } else if (error?.error) {
      originalMessage =
        error.error.message ?? error.error.error ?? 'Unknown error';
      type = 'HttpError';
    } else if (error?.message) {
      originalMessage = error.message;
      type = 'ObjectError';
    }

    // Categorize errors and provide user-friendly messages
    if (this.isNetworkError(error, originalMessage)) {
      message =
        'Network connection issue. Please check your internet connection.';
      type = 'NetworkError';
    } else if (this.isValidationError(error, originalMessage)) {
      message = 'Please check your input and try again.';
      type = 'ValidationError';
    } else if (this.isAuthenticationError(error, originalMessage)) {
      message = 'Authentication failed. Please log in again.';
      type = 'AuthError';
    } else if (this.isServerError(error, originalMessage)) {
      message = 'Server error occurred. Please try again later.';
      type = 'ServerError';
    } else if (this.isChunkLoadError(originalMessage)) {
      message = 'Application update detected. Please refresh the page.';
      type = 'ChunkLoadError';
    } else if (originalMessage.length > 0 && originalMessage.length < 100) {
      // Use original message if it's reasonable length and descriptive
      message = originalMessage;
    }

    // Create a key for error deduplication
    const key = `${type}_${message.substring(0, 50)}`;

    return { message, type, key };
  }

  private isNetworkError(error: any, message: string): boolean {
    return (
      error?.status === 0 ||
      error?.status >= 500 ||
      message.toLowerCase().includes('network') ||
      message.toLowerCase().includes('connection') ||
      message.toLowerCase().includes('timeout') ||
      error?.name === 'NetworkError'
    );
  }

  private isValidationError(error: any, message: string): boolean {
    return (
      error?.status === 400 ||
      message.toLowerCase().includes('validation') ||
      message.toLowerCase().includes('invalid') ||
      message.toLowerCase().includes('required')
    );
  }

  private isAuthenticationError(error: any, message: string): boolean {
    return (
      error?.status === 401 ||
      error?.status === 403 ||
      message.toLowerCase().includes('unauthorized') ||
      message.toLowerCase().includes('forbidden') ||
      message.toLowerCase().includes('authentication')
    );
  }

  private isServerError(error: any, message: string): boolean {
    return (
      error?.status >= 500 ||
      message.toLowerCase().includes('internal server error') ||
      message.toLowerCase().includes('service unavailable')
    );
  }

  private isChunkLoadError(message: string): boolean {
    return (
      message.toLowerCase().includes('loading chunk') ||
      message.toLowerCase().includes('loading css chunk') ||
      message.toLowerCase().includes('loading js chunk') ||
      message.toLowerCase().includes('chunkloaderror')
    );
  }

  private shouldSuppressError(errorKey: string): boolean {
    const now = Date.now();
    const cached = this.errorCache.get(errorKey);

    if (!cached) {
      return false;
    }

    // Reset cache if outside time window
    if (now - cached.lastSeen > this.ERROR_CACHE_WINDOW) {
      this.errorCache.delete(errorKey);
      return false;
    }

    // Suppress if we've seen this error too many times
    return cached.count >= this.MAX_SAME_ERROR_COUNT;
  }

  private updateErrorCache(errorKey: string): void {
    const now = Date.now();
    const cached = this.errorCache.get(errorKey);

    if (!cached) {
      this.errorCache.set(errorKey, { count: 1, lastSeen: now });
    } else {
      cached.count++;
      cached.lastSeen = now;
    }

    // Clean up old entries
    this.cleanupErrorCache();
  }

  private cleanupErrorCache(): void {
    const now = Date.now();
    for (const [key, value] of this.errorCache.entries()) {
      if (now - value.lastSeen > this.ERROR_CACHE_WINDOW) {
        this.errorCache.delete(key);
      }
    }
  }

  private showUserFriendlyError(errorInfo: {
    message: string;
    type: string;
  }): void {
    let duration = 8000; // Default 8 seconds for errors

    // Adjust duration based on error type
    switch (errorInfo.type) {
      case 'NetworkError':
      case 'ServerError':
        duration = 10000; // Longer for more serious errors
        break;
      case 'ValidationError':
        duration = 6000; // Shorter for validation errors
        break;
      case 'ChunkLoadError':
        duration = 15000; // Much longer for chunk load errors
        break;
    }

    this.toastService.error(errorInfo.message, duration, true);
  }

  private logToExternalService(
    originalError: any,
    errorInfo: any,
    context?: ErrorContext,
  ): void {
    // This is where you would implement logging to external services
    // like Sentry, LogRocket, or your own logging API

    const logData = {
      timestamp: new Date().toISOString(),
      error: {
        message: originalError?.message || 'Unknown error',
        stack: originalError?.stack,
        name: originalError?.name,
        type: errorInfo.type,
      },
      context: {
        url: window.location.href,
        userAgent: navigator.userAgent,
        timestamp: new Date(),
        ...context,
      },
      userFriendlyMessage: errorInfo.message,
    };

    // Example: Send to your logging endpoint
    // this.http.post('/api/logs/client-errors', logData).subscribe({
    //   error: (err) => console.warn('Failed to log error to server:', err)
    // });

    // For now, just log to console in development
    // Note: environment check would need to be implemented in consuming applications
    console.group('🚨 Global Error Handler - Detailed Log');
    console.error('Original Error:', originalError);
    console.log('Error Info:', errorInfo);
    console.log('Context:', context);
    console.log('Log Data:', logData);
    console.groupEnd();
  }
}

// Global injector instance - will be set by the application
let globalInjector: Injector | null = null;

// Function to set the global injector (call this in main.ts after bootstrapping)
export function setGlobalInjector(injector: Injector): void {
  globalInjector = injector;
}

// Utility function to manually handle errors with context
export function handleError(error: any, context?: ErrorContext): void {
  try {
    if (globalInjector) {
      const errorHandler = globalInjector.get(GlobalErrorHandler);
      errorHandler.handleError(error, context);
    } else {
      // Fallback if injector not available
      console.error('GlobalErrorHandler not available - injector not set');
      console.error('Original error:', error, context);

      // Try to show a basic alert as fallback
      if (typeof window !== 'undefined') {
        console.error('Error occurred:', error?.message || error);
      }
    }
  } catch (initError) {
    // Fallback if service not available
    console.error('GlobalErrorHandler not available:', initError);
    console.error('Original error:', error, context);
  }
}

// Decorator for automatic error handling in components/services
export function HandleErrors(context?: Partial<ErrorContext>) {
  return function (
    target: any,
    propertyKey: string,
    descriptor: PropertyDescriptor,
  ) {
    const originalMethod = descriptor.value;

    descriptor.value = function (...args: any[]) {
      try {
        const result = originalMethod.apply(this, args);

        // Handle async methods
        if (result && typeof result.catch === 'function') {
          return result.catch((error: any) => {
            handleError(error, {
              component: target.constructor.name,
              action: propertyKey,
              ...context,
            });
            throw error; // Re-throw to maintain normal error flow
          });
        }

        return result;
      } catch (error) {
        handleError(error, {
          component: target.constructor.name,
          action: propertyKey,
          ...context,
        });
        throw error; // Re-throw to maintain normal error flow
      }
    };

    return descriptor;
  };
}
