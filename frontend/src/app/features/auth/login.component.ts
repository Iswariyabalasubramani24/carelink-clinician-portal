import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { AuthService } from '../../core/services/auth.service';
import { CountryLanguageSelectorComponent } from './country-language-selector/country-language-selector.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, CountryLanguageSelectorComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  loading = false;
  errorMessage: string | null = null;

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly translate: TranslateService
  ) {}

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = null;
    const { email, password } = this.form.getRawValue();

    this.authService.login(email, password).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigateByUrl('/dashboard');
      },
      error: (err: HttpErrorResponse) => {
        this.loading = false;
        if (err.status === 403) {
          // A suspended account is a distinct, deliberate error - unlike wrong
          // credentials, surfacing it isn't a user-enumeration risk since the
          // clinician already proved they know the correct password.
          this.errorMessage = this.translate.instant('login.accountSuspended');
        } else {
          // The backend intentionally returns one generic message for both a
          // wrong password and a non-existent email (avoids user enumeration),
          // so the localized equivalent is shown here regardless of server text.
          this.errorMessage = this.translate.instant('login.invalidCredentials');
        }
      }
    });
  }
}
