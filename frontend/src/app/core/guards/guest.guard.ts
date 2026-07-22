import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';

// Inverse of authGuard: the login page is for signed-out visitors only. An
// already-authenticated clinician landing on /login (deep link, back button)
// is sent to their home instead of being shown a redundant sign-in form
// underneath the app header.
export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return true;
  }

  const isSuperAdmin = authService.getCurrentClinician()?.role === 'SuperAdmin';
  return router.createUrlTree([isSuperAdmin ? '/hospitals' : '/dashboard']);
};
