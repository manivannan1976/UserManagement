import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';

function isConfiguredApiRequest(requestUrl: string): boolean {
  const configuredUrl = environment.apiBaseUrl;
  const configuredPath = configuredUrl.replace(/\/$/, '');

  if (!/^https?:\/\//i.test(configuredUrl)) {
    const requestPath = requestUrl.split(/[?#]/, 1)[0];
    return (
      !/^(?:[a-z][a-z\d+.-]*:)?\/\//i.test(requestUrl) &&
      !/^[a-z][a-z\d+.-]*:/i.test(requestUrl) &&
      (requestPath === configuredPath || requestPath.startsWith(`${configuredPath}/`))
    );
  }

  return (
    requestUrl === configuredPath ||
    requestUrl.startsWith(`${configuredPath}/`) ||
    requestUrl.startsWith(`${configuredPath}?`) ||
    requestUrl.startsWith(`${configuredPath}#`)
  );
}

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const router = inject(Router);

  return next(request).pipe(
    catchError((error: unknown) => {
      const isPublicAuthEndpoint =
        /\/api\/(?:auth\/(?:login|csrf|session|logout)|invitations\/)/.test(request.url);
      if (error instanceof HttpErrorResponse &&
          isConfiguredApiRequest(request.url) &&
          !isPublicAuthEndpoint) {
        if (error.status === 401) {
          void router.navigate(['/login'], {
            queryParams: { reauth: 'true', returnUrl: router.url }
          });
        } else if (error.status === 403) {
          void router.navigate(['/unauthorized']);
        }
      }

      return throwError(() => error);
    })
  );
};
