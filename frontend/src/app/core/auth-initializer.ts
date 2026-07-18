import { firstValueFrom } from 'rxjs';

import { AuthService } from './services/auth.service';

// Attempts a silent refresh on app bootstrap using the httpOnly refresh-token
// cookie, so a page reload restores the session before route guards evaluate.
// A failed refresh (no valid cookie) is expected for a logged-out visitor and
// is swallowed here rather than surfaced as an error.
export function initializeAuth(authService: AuthService): () => Promise<void> {
  return () =>
    firstValueFrom(authService.refresh())
      .then(() => undefined)
      .catch(() => undefined);
}
