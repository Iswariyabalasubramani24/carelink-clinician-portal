import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { filter, map, startWith } from 'rxjs/operators';

import { Clinician } from './core/models/auth.model';
import { AuthService } from './core/services/auth.service';
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

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  onLogout(): void {
    this.authService.logout().subscribe({
      next: () => this.router.navigateByUrl('/login'),
      error: () => this.router.navigateByUrl('/login')
    });
  }
}
