import { firstValueFrom } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';

export const SUPPORTED_LANGUAGES = ['en', 'fr', 'de', 'es'];
export const DEFAULT_LANGUAGE = 'en';
export const LANGUAGE_STORAGE_KEY = 'carelink-language';

export const LANGUAGE_LABELS: Record<string, string> = {
  en: 'English',
  fr: 'Français',
  de: 'Deutsch',
  es: 'Español'
};

export function getStoredLanguage(): string {
  const saved = localStorage.getItem(LANGUAGE_STORAGE_KEY);
  return saved && SUPPORTED_LANGUAGES.includes(saved) ? saved : DEFAULT_LANGUAGE;
}

// Loads the persisted (or default) language's translations before the app
// renders, avoiding a flash of untranslated / wrong-language content.
export function initializeTranslations(translate: TranslateService): () => Promise<void> {
  return () => {
    translate.addLangs(SUPPORTED_LANGUAGES);
    translate.setDefaultLang(DEFAULT_LANGUAGE);

    return firstValueFrom(translate.use(getStoredLanguage())).then(() => undefined);
  };
}
