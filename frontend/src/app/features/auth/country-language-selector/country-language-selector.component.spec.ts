import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';

import { COUNTRY_STORAGE_KEY } from '../../../core/country-language';
import { LANGUAGE_STORAGE_KEY } from '../../../core/i18n';
import { Tenant } from '../../../core/models/tenant.model';
import { TenantService } from '../../../core/services/tenant.service';
import { CountryLanguageSelectorComponent } from './country-language-selector.component';

describe('CountryLanguageSelectorComponent', () => {
  let fixture: ComponentFixture<CountryLanguageSelectorComponent>;
  let component: CountryLanguageSelectorComponent;
  let tenantServiceMock: { getAll: jest.Mock };
  let translate: TranslateService;

  // Order matters here: it mirrors the backend, which orders tenants by name
  // ("Apollo Hospital" before "Charite Hospital"), so India is regions[0].
  const tenants: Tenant[] = [
    { id: 1, name: 'Apollo Hospital', region: 'India', languageCode: 'en' },
    { id: 2, name: 'Charite Hospital', region: 'Germany', languageCode: 'de' }
  ];

  function setLocale(locale: string): void {
    Object.defineProperty(window.navigator, 'language', { value: locale, configurable: true });
  }

  async function setup(): Promise<void> {
    tenantServiceMock = { getAll: jest.fn().mockReturnValue(of(tenants)) };

    await TestBed.configureTestingModule({
      imports: [CountryLanguageSelectorComponent],
      providers: [provideTranslateService(), { provide: TenantService, useValue: tenantServiceMock }]
    }).compileComponents();

    translate = TestBed.inject(TranslateService);
    translate.addLangs(['en', 'fr', 'de', 'es']);
    translate.setDefaultLang('en');

    fixture = TestBed.createComponent(CountryLanguageSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(() => {
    localStorage.clear();
  });

  describe('auto-detect mapping', () => {
    it('preselects the country matching the browser locale', async () => {
      setLocale('de-DE');
      await setup();

      expect(component.selectedRegion).toBe('Germany');
    });

    it('falls back to the first available region when the locale matches no tenant', async () => {
      setLocale('ja-JP');
      await setup();

      expect(component.selectedRegion).toBe('India');
    });

    it('falls back to the first available region when the locale has no country subtag', async () => {
      setLocale('en');
      await setup();

      expect(component.selectedRegion).toBe('India');
    });
  });

  describe('language options based on country', () => {
    it('offers only English for a country whose default language already is English', async () => {
      setLocale('en-IN');
      await setup();

      expect(component.languageOptions.map((o) => o.code)).toEqual(['en']);
    });

    it('offers the country default plus English for a non-English country', async () => {
      setLocale('de-DE');
      await setup();

      expect(component.languageOptions.map((o) => o.code)).toEqual(['de', 'en']);
    });

    it('recomputes language options when the country is changed manually', async () => {
      setLocale('en-IN');
      await setup();
      expect(component.languageOptions.map((o) => o.code)).toEqual(['en']);

      component.onCountryChange('Germany');

      expect(component.languageOptions.map((o) => o.code)).toEqual(['de', 'en']);
    });
  });

  describe('default selection behavior', () => {
    it('defaults the language to English when the country was only auto-detected', async () => {
      setLocale('de-DE');
      await setup();

      expect(component.selectedRegion).toBe('Germany');
      expect(component.selectedLanguage).toBe('en');
    });

    it('switches the default language to the country default once the country is changed manually', async () => {
      setLocale('en-IN');
      await setup();
      expect(component.selectedLanguage).toBe('en');

      component.onCountryChange('Germany');

      expect(component.selectedLanguage).toBe('de');
      expect(translate.currentLang).toBe('de');
    });

    it('keeps a manually chosen language regardless of the country default', async () => {
      setLocale('de-DE');
      await setup();

      component.onLanguageChange('en');

      expect(component.selectedLanguage).toBe('en');
      expect(translate.currentLang).toBe('en');
    });

    it('persists the country and language choices to localStorage', async () => {
      setLocale('en-IN');
      await setup();

      component.onCountryChange('Germany');

      expect(localStorage.getItem(COUNTRY_STORAGE_KEY)).toBe('Germany');
      expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe('de');
    });

    it('restores a previously stored country as already-resolved on a later visit', async () => {
      localStorage.setItem(COUNTRY_STORAGE_KEY, 'Germany');
      setLocale('en-IN');
      await setup();

      // A stored country is treated like a manual choice: language follows
      // that country's default rather than resetting to English.
      expect(component.selectedRegion).toBe('Germany');
      expect(component.selectedLanguage).toBe('de');
    });
  });

  describe('rendered DOM select values (regression: async-populated <select> options)', () => {
    it('the native <select> elements reflect a restored preference, not just the component fields', async () => {
      localStorage.setItem(COUNTRY_STORAGE_KEY, 'Germany');
      localStorage.setItem(LANGUAGE_STORAGE_KEY, 'de');
      setLocale('en-IN');
      await setup();

      const countrySelect = fixture.nativeElement.querySelector('#country') as HTMLSelectElement;
      const languageSelect = fixture.nativeElement.querySelector('#selectorLanguage') as HTMLSelectElement;

      expect(countrySelect.value).toBe('Germany');
      expect(languageSelect.value).toBe('de');
    });

    it('the native country <select> reflects a manual change made via user interaction', async () => {
      setLocale('en-IN');
      await setup();

      const countrySelect = fixture.nativeElement.querySelector('#country') as HTMLSelectElement;
      countrySelect.value = 'Germany';
      countrySelect.dispatchEvent(new Event('change'));
      fixture.detectChanges();

      const languageSelect = fixture.nativeElement.querySelector('#selectorLanguage') as HTMLSelectElement;
      expect(countrySelect.value).toBe('Germany');
      expect(languageSelect.value).toBe('de');
    });
  });
});
