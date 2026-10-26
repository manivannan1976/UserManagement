import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { CsrfTokenStore } from './csrf-token.store';

export const apiSessionInterceptor: HttpInterceptorFn = (request, next) => {
  const configuredUrl = environment.apiBaseUrl.replace(/\/$/, '');
  const isApiRequest = request.url.startsWith(configuredUrl);
  if (!isApiRequest) {
    return next(request);
  }

  let apiRequest = request.clone({ withCredentials: true });
  const csrfToken = inject(CsrfTokenStore).get();
  if (csrfToken && !['GET', 'HEAD', 'OPTIONS'].includes(request.method.toUpperCase())) {
    apiRequest = apiRequest.clone({ setHeaders: { 'X-CSRF-TOKEN': csrfToken } });
  }

  return next(apiRequest);
};
