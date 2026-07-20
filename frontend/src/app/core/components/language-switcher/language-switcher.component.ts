import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  DEFAULT_LANGUAGE,
  LANGUAGE_LABELS,
  LANGUAGE_STORAGE_KEY,
  SUPPORTED_LANGUAGES
} from '../../i18n';

interface LanguageOption {
  code: string;
  label: string;
}

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './language-switcher.component.html',
  styleUrl: './language-switcher.component.scss'
})
export class LanguageSwitcherComponent {
  readonly languages: LanguageOption[] = SUPPORTED_LANGUAGES.map((code) => ({
    code,
    label: LANGUAGE_LABELS[code]
  }));

  constructor(readonly translate: TranslateService) {}

  get currentLang(): string {
    return this.translate.currentLang || this.translate.getDefaultLang() || DEFAULT_LANGUAGE;
  }

  onLanguageChange(event: Event): void {
    const lang = (event.target as HTMLSelectElement).value;
    if (!SUPPORTED_LANGUAGES.includes(lang)) {
      return;
    }

    this.translate.use(lang);
    localStorage.setItem(LANGUAGE_STORAGE_KEY, lang);
  }
}
