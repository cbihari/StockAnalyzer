import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../environments/environment';
import { ClientIdentityService } from '../core/client-identity.service';
import { AuthTokenStore } from './auth-token.store';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const tokenStore = inject(AuthTokenStore);
  const identity = inject(ClientIdentityService);
  const isApiRequest = request.url.startsWith(environment.apiUrl) || request.url.startsWith('/api/');
  if (!isApiRequest) return next(request);

  const headers: Record<string, string> = { 'X-Client-ID': identity.id };
  if (tokenStore.token()) headers['Authorization'] = `Bearer ${tokenStore.token()}`;
  return next(request.clone({ setHeaders: headers }));
};
