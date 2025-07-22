import { HttpInterceptorFn } from '@angular/common/http';

/**
 * HTTP interceptor that adds credentials (cookies) to all outgoing requests
 * and sets the default Content-Type header to application/json.
 */
export const credentialsInterceptor: HttpInterceptorFn = (req, next) => {
  const reqWithCredentials = req.clone({
    setHeaders: {
      'Content-Type': 'application/json',
    },
    withCredentials: true,
  });

  return next(reqWithCredentials);
};
