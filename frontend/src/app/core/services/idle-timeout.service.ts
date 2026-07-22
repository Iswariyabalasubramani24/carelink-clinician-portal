import { DestroyRef, Injectable, NgZone, inject } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from './auth.service';

// Signs the clinician out after a period of inactivity while the app is open,
// mirroring clinical-workstation session policies. Pairs with the backend's
// sliding refresh-token window, which covers the closed-browser case.
export const IDLE_TIMEOUT_MS = 15 * 60 * 1000;

const ACTIVITY_EVENTS = ['mousedown', 'mousemove', 'keydown', 'scroll', 'touchstart'] as const;

// Re-arming a timer on every mousemove would thrash; activity within this
// interval after the last reset is coalesced into one re-arm.
const RESET_COALESCE_MS = 30 * 1000;

@Injectable({ providedIn: 'root' })
export class IdleTimeoutService {
  private timeoutHandle: ReturnType<typeof setTimeout> | null = null;
  private lastReset = 0;
  private watching = false;

  private readonly onActivity = () => this.resetTimer();

  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly zone: NgZone
  ) {}

  start(): void {
    this.authService.isAuthenticated$.subscribe((authenticated) => {
      if (authenticated) {
        this.watch();
      } else {
        this.unwatch();
      }
    });

    this.destroyRef.onDestroy(() => this.unwatch());
  }

  private watch(): void {
    if (this.watching) {
      return;
    }
    this.watching = true;

    // Activity listeners run outside Angular so mousemove doesn't trigger
    // change detection; only the actual timeout re-enters the zone.
    this.zone.runOutsideAngular(() => {
      for (const event of ACTIVITY_EVENTS) {
        document.addEventListener(event, this.onActivity, { passive: true });
      }
    });

    this.lastReset = 0;
    this.resetTimer();
  }

  private unwatch(): void {
    if (!this.watching) {
      return;
    }
    this.watching = false;

    for (const event of ACTIVITY_EVENTS) {
      document.removeEventListener(event, this.onActivity);
    }
    if (this.timeoutHandle !== null) {
      clearTimeout(this.timeoutHandle);
      this.timeoutHandle = null;
    }
  }

  private resetTimer(): void {
    const now = Date.now();
    if (now - this.lastReset < RESET_COALESCE_MS) {
      return;
    }
    this.lastReset = now;

    if (this.timeoutHandle !== null) {
      clearTimeout(this.timeoutHandle);
    }

    this.zone.runOutsideAngular(() => {
      this.timeoutHandle = setTimeout(() => this.zone.run(() => this.onIdleTimeout()), IDLE_TIMEOUT_MS);
    });
  }

  private onIdleTimeout(): void {
    this.unwatch();
    this.authService.logout().subscribe({
      complete: () => this.router.navigate(['/login'], { queryParams: { reason: 'idle' } }),
      error: () => this.router.navigate(['/login'], { queryParams: { reason: 'idle' } })
    });
  }
}
