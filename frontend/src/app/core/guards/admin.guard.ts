import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';

// APP_INITIALIZER awaits the silent refresh before the router evaluates any
// guard, so the clinician's role is already known here even on a hard reload.
export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.getCurrentClinician()?.role === 'Admin') {
    return true;
  }

  return router.createUrlTree(['/dashboard']);
};
