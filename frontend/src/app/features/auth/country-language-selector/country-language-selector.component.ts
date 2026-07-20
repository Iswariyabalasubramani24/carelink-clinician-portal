import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  COUNTRY_STORAGE_KEY,
  detectRegionFromLocale
} from '../../../core/country-language';
import {
  DEFAULT_LANGUAGE,
  LANGUAGE_LABELS,
  LANGUAGE_STORAGE_KEY,
  SUPPORTED_LANGUAGES
} from '../../../core/i18n';
import { Tenant } from '../../../core/models/tenant.model';
import { TenantService } from '../../../core/services/tenant.service';

interface RegionOption {
  region: string;
  languageCode: string;
}

interface LanguageOption {
  code: string;
  label: string;
}

@Component({
  selector: 'app-country-language-selector',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './country-language-selector.component.html',
  styleUrl: './country-language-selector.component.scss'
})
export class CountryLanguageSelectorComponent implements OnInit {
  regions: RegionOption[] = [];
  selectedRegion: string | null = null;
  languageOptions: LanguageOption[] = [];
  selectedLanguage = DEFAULT_LANGUAGE;

  // Bound to the <select> elements via ReactiveFormsModule instead of a
  // plain [value] binding. A plain [value] binding on a <select> whose
  // <option>s are populated asynchronously (from the tenants HTTP call) is
  // an Angular/browser interaction that doesn't reliably re-sync the native
  // element's selection; SelectControlValueAccessor (activated by
  // [formControl]) handles that synchronization correctly regardless of
  // when the options are added.
  readonly countryControl = new FormControl<string | null>(null);
  readonly languageControl = new FormControl<string>(DEFAULT_LANGUAGE, { nonNullable: true });

  // Tracks whether the country came from a deliberate user pick (or a
  // restored preference) rather than a geo-guess, per the agreed rule:
  // auto-detected country -> language defaults to English until touched;
  // a manually chosen country -> language defaults to that country's own.
  private countryManuallyChanged = false;

  constructor(
    private readonly tenantService: TenantService,
    private readonly translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.tenantService.getAll().subscribe((tenants) => {
      this.regions = this.toRegionOptions(tenants);
      this.initSelection();

      // Only react to control changes once the initial (restored/detected)
      // selection has been applied, so that silent programmatic set above
      // doesn't loop back into the change handlers below.
      this.countryControl.valueChanges.subscribe((region) => {
        if (region) {
          this.onCountryChange(region);
        }
      });
      this.languageControl.valueChanges.subscribe((lang) => {
        if (lang) {
          this.onLanguageChange(lang);
        }
      });
    });
  }

  onCountryChange(region: string): void {
    if (!this.regions.some((r) => r.region === region)) {
      return;
    }

    this.selectedRegion = region;
    this.countryControl.setValue(region, { emitEvent: false });
    this.countryManuallyChanged = true;
    this.updateLanguageOptions();

    const regionLanguage = this.currentRegionLanguage();
    this.applyLanguage(regionLanguage ?? DEFAULT_LANGUAGE);

    localStorage.setItem(COUNTRY_STORAGE_KEY, region);
  }

  onLanguageChange(lang: string): void {
    if (!this.languageOptions.some((option) => option.code === lang)) {
      return;
    }

    this.applyLanguage(lang);
  }

  private toRegionOptions(tenants: Tenant[]): RegionOption[] {
    const byRegion = new Map<string, string>();
    for (const tenant of tenants) {
      if (!byRegion.has(tenant.region)) {
        byRegion.set(tenant.region, tenant.languageCode);
      }
    }
    return Array.from(byRegion.entries()).map(([region, languageCode]) => ({ region, languageCode }));
  }

  private initSelection(): void {
    const storedCountry = localStorage.getItem(COUNTRY_STORAGE_KEY);
    const storedLanguage = localStorage.getItem(LANGUAGE_STORAGE_KEY);

    if (storedCountry && this.regions.some((r) => r.region === storedCountry)) {
      this.selectedRegion = storedCountry;
      this.countryManuallyChanged = true;
    } else {
      const detected = detectRegionFromLocale(
        navigator.language,
        this.regions.map((r) => r.region)
      );
      this.selectedRegion = detected ?? this.regions[0]?.region ?? null;
      this.countryManuallyChanged = false;
    }

    this.countryControl.setValue(this.selectedRegion, { emitEvent: false });
    this.updateLanguageOptions();

    if (storedLanguage && this.languageOptions.some((option) => option.code === storedLanguage)) {
      this.selectedLanguage = storedLanguage;
    } else if (this.countryManuallyChanged) {
      this.selectedLanguage = this.currentRegionLanguage() ?? DEFAULT_LANGUAGE;
    } else {
      this.selectedLanguage = DEFAULT_LANGUAGE;
    }

    this.languageControl.setValue(this.selectedLanguage, { emitEvent: false });
    this.translate.use(this.selectedLanguage);
  }

  private currentRegionLanguage(): string | null {
    const region = this.regions.find((r) => r.region === this.selectedRegion);
    if (!region) {
      return null;
    }
    return SUPPORTED_LANGUAGES.includes(region.languageCode) ? region.languageCode : null;
  }

  private updateLanguageOptions(): void {
    const regionLanguage = this.currentRegionLanguage();
    const codes = regionLanguage && regionLanguage !== DEFAULT_LANGUAGE ? [regionLanguage, DEFAULT_LANGUAGE] : [DEFAULT_LANGUAGE];

    this.languageOptions = codes.map((code) => ({ code, label: LANGUAGE_LABELS[code] }));
  }

  private applyLanguage(lang: string): void {
    this.selectedLanguage = lang;
    this.languageControl.setValue(lang, { emitEvent: false });
    this.translate.use(lang);
    localStorage.setItem(LANGUAGE_STORAGE_KEY, lang);
  }
}
