import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';

import { AuthService } from './core/services/auth.service';
import { useEnglishTestTranslations } from './core/testing/translate-testing';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        provideTranslateService(),
        { provide: AuthService, useValue: { currentClinician$: of(null), logout: jest.fn() } },
        {
          provide: Router,
          useValue: { navigateByUrl: jest.fn(), events: of(), url: '/patients' }
        }
      ]
    }).compileComponents();

    useEnglishTestTranslations(TestBed.inject(TranslateService));
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it(`should have the 'frontend' title`, () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app.title).toEqual('frontend');
  });

  it('should render title', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Med Clinician Portal');
  });
});
