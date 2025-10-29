import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';

export const authGuard: CanActivateFn = () => {
  const token = localStorage.getItem('tufo.jwt');
  if (token) return true;

  const router = inject(Router);
  router.navigate(['/login']);
  return false;
};