import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../../core/services/auth.service';
import { useEnglishTestTranslations } from '../../../core/testing/translate-testing';
import { ChangePasswordComponent } from './change-password.component';

describe('ChangePasswordComponent', () => {
  let fixture: ComponentFixture<ChangePasswordComponent>;
  let component: ChangePasswordComponent;
  let authServiceMock: { changePassword: jest.Mock; getCurrentClinician: jest.Mock };
  let routerMock: { navigateByUrl: jest.Mock };

  function submitForm(): void {
    fixture.debugElement.query(By.css('form')).triggerEventHandler('ngSubmit', null);
    fixture.detectChanges();
  }

  beforeEach(async () => {
    authServiceMock = {
      changePassword: jest.fn(),
      getCurrentClinician: jest.fn().mockReturnValue({ role: 'Clinician' })
    };
    routerMock = { navigateByUrl: jest.fn() };

    await TestBed.configureTestingModule({
      imports: [ChangePasswordComponent],
      providers: [
        provideTranslateService(),
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    }).compileComponents();

    useEnglishTestTranslations(TestBed.inject(TranslateService));

    fixture = TestBed.createComponent(ChangePasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders current, new, and confirm password fields', () => {
    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('#currentPassword')).toBeTruthy();
    expect(el.querySelector('#newPassword')).toBeTruthy();
    expect(el.querySelector('#confirmPassword')).toBeTruthy();
  });

  it('rejects mismatched new passwords without calling the API', () => {
    component.form.setValue({
      currentPassword: 'Temp#Passw0rd',
      newPassword: 'MyNew#Passw0rd',
      confirmPassword: 'Different#Passw0rd'
    });
    submitForm();

    const text = (fixture.debugElement.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Passwords do not match.');
    expect(authServiceMock.changePassword).not.toHaveBeenCalled();
  });

  it('rejects a too-short new password without calling the API', () => {
    component.form.setValue({
      currentPassword: 'Temp#Passw0rd',
      newPassword: 'short',
      confirmPassword: 'short'
    });
    submitForm();

    const text = (fixture.debugElement.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('The new password must be at least 8 characters long.');
    expect(authServiceMock.changePassword).not.toHaveBeenCalled();
  });

  it('submits and shows the success banner', () => {
    authServiceMock.changePassword.mockReturnValue(of(void 0));

    component.form.setValue({
      currentPassword: 'Temp#Passw0rd',
      newPassword: 'MyNew#Passw0rd',
      confirmPassword: 'MyNew#Passw0rd'
    });
    submitForm();

    expect(authServiceMock.changePassword).toHaveBeenCalledWith('Temp#Passw0rd', 'MyNew#Passw0rd');
    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('.banner--success')).toBeTruthy();
    expect(el.textContent).toContain('Your password has been changed.');
  });

  it('shows the wrong-current-password error on a 400 response', () => {
    authServiceMock.changePassword.mockReturnValue(throwError(() => ({ status: 400 })));

    component.form.setValue({
      currentPassword: 'Wrong#Passw0rd',
      newPassword: 'MyNew#Passw0rd',
      confirmPassword: 'MyNew#Passw0rd'
    });
    submitForm();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('.banner--error')).toBeTruthy();
    expect(el.textContent).toContain('The current password is incorrect.');
  });

  it('navigates back to the dashboard for a regular clinician', () => {
    component.onBack();
    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/dashboard');
  });
});
