import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { Observable, combineLatest } from 'rxjs';
import { filter, map, startWith } from 'rxjs/operators';

import { Clinician } from './core/models/auth.model';
import { AuthService } from './core/services/auth.service';
import { IdleTimeoutService } from './core/services/idle-timeout.service';
import { LanguageSwitcherComponent } from './core/components/language-switcher/language-switcher.component';
import { ClinicSwitcherComponent } from './core/components/clinic-switcher/clinic-switcher.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    TranslateModule,
    LanguageSwitcherComponent,
    ClinicSwitcherComponent
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'frontend';

  currentClinician$: Observable<Clinician | null> = this.authService.currentClinician$;

  // The login page has its own combined country/language selector, so the
  // header switcher would just be a redundant second control there.
  isLoginRoute$: Observable<boolean> = this.router.events.pipe(
    filter((event): event is NavigationEnd => event instanceof NavigationEnd),
    map((event) => event.url.startsWith('/login')),
    startWith(this.router.url.startsWith('/login'))
  );

  // Drives the authenticated header chrome (nav + session controls). Gated on
  // the route as well as the session so the chrome never overlays the login
  // page, even in transitional states (e.g. mid-logout navigation).
  chromeClinician$: Observable<Clinician | null> = combineLatest([
    this.currentClinician$,
    this.isLoginRoute$
  ]).pipe(map(([clinician, isLogin]) => (isLogin ? null : clinician)));

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    idleTimeout: IdleTimeoutService
  ) {
    // Auto-logout after inactivity, HIPAA-style: the service watches
    // authentication state itself and only runs while signed in.
    idleTimeout.start();
  }

  onLogout(): void {
    this.authService.logout().subscribe({
      next: () => this.router.navigateByUrl('/login'),
      error: () => this.router.navigateByUrl('/login')
    });
  }
}
