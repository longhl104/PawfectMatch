/* eslint-disable @typescript-eslint/no-explicit-any */
import { Injectable, inject } from '@angular/core';
import {
  GlobalErrorHandler,
  ErrorContext,
} from './global-error-handler.service';

/**
 * Error handling service that can be injected into components and services
 * This provides a simpler alternative to the utility functions for manual error handling
 */
@Injectable({
  providedIn: 'root',
})
export class ErrorHandlingService {
  private globalErrorHandler = inject(GlobalErrorHandler);

  /**
   * Handle an error with optional context
   * This is the preferred method for manual error handling in components/services
   */
  handleError(error: any, context?: ErrorContext): void {
    this.globalErrorHandler.handleError(error, context);
  }

  /**
   * Handle an error with automatic component context detection
   * Pass 'this' as the component parameter for automatic context
   */
  handleErrorWithComponent(
    error: any,
    component: any,
    action?: string,
    additionalContext?: Partial<ErrorContext>,
  ): void {
    const context: ErrorContext = {
      component: component.constructor?.name || 'Unknown',
      action: action,
      url: window.location.href,
      timestamp: new Date(),
      ...additionalContext,
    };

    this.globalErrorHandler.handleError(error, context);
  }

  /**
   * Handle async operations with automatic error handling
   * Returns a promise that will handle errors automatically
   */
  async handleAsync<T>(
    operation: () => Promise<T>,
    context?: ErrorContext,
  ): Promise<T | undefined> {
    try {
      return await operation();
    } catch (error) {
      this.handleError(error, context);
      return undefined;
    }
  }

  /**
   * Wrap a function with error handling
   * Useful for event handlers and callbacks
   */
  wrapWithErrorHandling<T extends (...args: any[]) => any>(
    fn: T,
    context?: ErrorContext,
  ): T {
    return ((...args: any[]) => {
      try {
        const result = fn(...args);

        // Handle async results
        if (result && typeof result.catch === 'function') {
          return result.catch((error: any) => {
            this.handleError(error, context);
            throw error;
          });
        }

        return result;
      } catch (error) {
        this.handleError(error, context);
        throw error;
      }
    }) as T;
  }
}
