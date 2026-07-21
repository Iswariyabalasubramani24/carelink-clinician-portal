import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../core/services/auth.service';
import { TenantService } from '../../core/services/tenant.service';
import { useEnglishTestTranslations } from '../../core/testing/translate-testing';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let authServiceMock: { login: jest.Mock };
  let routerMock: { navigateByUrl: jest.Mock };
  let tenantServiceMock: { getAll: jest.Mock };

  function submitForm(): void {
    fixture.debugElement.query(By.css('form')).triggerEventHandler('ngSubmit', null);
    fixture.detectChanges();
  }

  beforeEach(async () => {
    authServiceMock = { login: jest.fn() };
    routerMock = { navigateByUrl: jest.fn() };
    tenantServiceMock = { getAll: jest.fn().mockReturnValue(of([])) };

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideTranslateService(),
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
        { provide: TenantService, useValue: tenantServiceMock }
      ]
    }).compileComponents();

    useEnglishTestTranslations(TestBed.inject(TranslateService));

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders the email and password fields plus a submit button', () => {
    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('#email')).toBeTruthy();
    expect(el.querySelector('#password')).toBeTruthy();
    expect(el.querySelector('button[type="submit"]')).toBeTruthy();
  });

  it('shows validation errors when submitted empty', () => {
    submitForm();

    const text = (fixture.debugElement.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Email is required.');
    expect(text).toContain('Password is required.');
    expect(authServiceMock.login).not.toHaveBeenCalled();
  });

  it('shows a validation error for an invalid email format', () => {
    component.form.setValue({ email: 'not-an-email', password: 'irrelevant' });
    submitForm();

    const text = (fixture.debugElement.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Enter a valid email address.');
    expect(authServiceMock.login).not.toHaveBeenCalled();
  });

  it('calls AuthService.login with form values and navigates to /patients on success', () => {
    authServiceMock.login.mockReturnValue(
      of({
        accessToken: 'token',
        accessTokenExpiresAt: new Date().toISOString(),
        clinicianId: 1,
        email: 'doctor@apollo.com',
        firstName: 'Anita',
        lastName: 'Rao',
        role: 'Clinician',
        tenantId: 1
      })
    );

    component.form.setValue({ email: 'doctor@apollo.com', password: 'Test@123' });
    submitForm();

    expect(authServiceMock.login).toHaveBeenCalledWith('doctor@apollo.com', 'Test@123');
    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/dashboard');
  });

  it('shows a clear, localized error message and does not navigate when login fails', () => {
    authServiceMock.login.mockReturnValue(
      throwError(() => ({ error: { error: 'some server-generated text' } }))
    );

    component.form.setValue({ email: 'doctor@apollo.com', password: 'WrongPassword' });
    submitForm();

    expect(component.errorMessage).toBe('Invalid email or password.');
    expect((fixture.debugElement.nativeElement as HTMLElement).textContent).toContain(
      'Invalid email or password.'
    );
    expect(routerMock.navigateByUrl).not.toHaveBeenCalled();
  });

  it('shows the distinct suspended-account message on a 403 response, not the generic credentials error', () => {
    authServiceMock.login.mockReturnValue(
      throwError(() => ({ status: 403, error: { error: 'Your account has been suspended.' } }))
    );

    component.form.setValue({ email: 'kavita.menon@apollo.com', password: 'Tmp#Passw0rd' });
    submitForm();

    expect(component.errorMessage).toBe(
      'Your account has been suspended. Please contact your clinic administrator.'
    );
    expect(routerMock.navigateByUrl).not.toHaveBeenCalled();
  });
});
