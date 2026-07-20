import { TranslateService } from '@ngx-translate/core';

import enTranslations from '../../../assets/i18n/en.json';

// Loads the real English translations synchronously into a test's TranslateService
// (backed by the default no-op loader), so specs assert against actual rendered
// copy instead of duplicating translation strings inline.
export function useEnglishTestTranslations(translate: TranslateService): void {
  translate.setTranslation('en', enTranslations);
  translate.addLangs(['en', 'fr', 'de', 'es']);
  translate.setDefaultLang('en');
  translate.use('en');
}
