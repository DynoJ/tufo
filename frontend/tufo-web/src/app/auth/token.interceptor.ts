import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';

export const tokenInterceptor: HttpInterceptorFn = (req, next) => {
  // Don’t attach Authorization header to auth endpoints
  const isAuthCall = req.url.includes('/api/auth/');
  if (!isAuthCall) {
    const token = localStorage.getItem('tufo.jwt');
    if (token) req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` }});
  }

  const router = inject(Router);
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) router.navigate(['/login']);
      return throwError(() => err);
    })
  );
};