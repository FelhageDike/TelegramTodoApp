import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const adminAuthInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/admin/api') || req.url.endsWith('/login') || req.url.endsWith('/auth/config')) {
    return next(req);
  }

  const auth = inject(AuthService);
  const token = auth.getAccessToken();
  const key = auth.getKey();

  const headers: Record<string, string> = {};
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  } else if (key) {
    headers['X-Admin-Key'] = key;
  }

  if (Object.keys(headers).length === 0) {
    return next(req);
  }

  return next(req.clone({ setHeaders: headers }));
};
