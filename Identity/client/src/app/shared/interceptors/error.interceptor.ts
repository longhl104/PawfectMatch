import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { GlobalErrorHandler } from '../services/global-error-handler.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const errorHandler = inject(GlobalErrorHandler);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Extract request context for better error reporting
      const context = {
        component: 'HttpInterceptor',
        action: `${req.method} ${req.url}`,
        url: req.url,
        timestamp: new Date(),
      };

      // Handle different types of HTTP errors
      if (error.error instanceof ErrorEvent) {
        // Client-side error
        errorHandler.handleError(error.error, {
          ...context,
          action: `Client Error - ${context.action}`,
        });
      } else {
        // Server-side error
        let errorMessage = 'Server error occurred';

        switch (error.status) {
          case 400:
            errorMessage =
              error.error?.error ??
              error.error?.message ??
              'Bad request. Please check your input.';
            break;
          case 401:
            errorMessage = 'You are not authorized. Please log in.';
            break;
          case 403:
            errorMessage = "Access forbidden. You don't have permission.";
            break;
          case 404:
            errorMessage = 'The requested resource was not found.';
            break;
          case 409:
            errorMessage =
              error.error?.error ??
              error.error?.message ??
              'Conflict occurred. The resource may already exist.';
            break;
          case 422:
            errorMessage =
              error.error?.error ??
              error.error?.message ??
              'Validation failed. Please check your input.';
            break;
          case 429:
            errorMessage = 'Too many requests. Please try again later.';
            break;
          case 500:
            errorMessage = 'Internal server error. Please try again later.';
            break;
          case 502:
            errorMessage =
              'Service temporarily unavailable. Please try again later.';
            break;
          case 503:
            errorMessage = 'Service unavailable. Please try again later.';
            break;
          default:
            if (error.status >= 500) {
              errorMessage = 'Server error occurred. Please try again later.';
            } else if (error.status >= 400) {
              errorMessage =
                error.error?.error ??
                error.error?.message ??
                'Request failed. Please try again.';
            }
        }

        // Create a custom error with user-friendly message
        const customError = new Error(errorMessage);
        (customError as any).originalError = error;
        (customError as any).status = error.status;

        errorHandler.handleError(customError, {
          ...context,
          action: `HTTP ${error.status} - ${context.action}`,
        });
      }

      // Always re-throw the error so components can handle it if needed
      return throwError(() => error);
    })
  );
};
