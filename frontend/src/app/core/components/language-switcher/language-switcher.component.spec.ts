import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';

import { LANGUAGE_STORAGE_KEY } from '../../i18n';
import { LanguageSwitcherComponent } from './language-switcher.component';

describe('LanguageSwitcherComponent', () => {
  let fixture: ComponentFixture<LanguageSwitcherComponent>;
  let component: LanguageSwitcherComponent;
  let translate: TranslateService;

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [LanguageSwitcherComponent],
      providers: [provideTranslateService()]
    }).compileComponents();

    translate = TestBed.inject(TranslateService);
    translate.addLangs(['en', 'fr', 'de', 'es']);
    translate.setDefaultLang('en');
    translate.use('en');

    fixture = TestBed.createComponent(LanguageSwitcherComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders all four supported language options', () => {
    const options = fixture.debugElement.queryAll(By.css('option'));
    const values = options.map((o) => (o.nativeElement as HTMLOptionElement).value);

    expect(values).toEqual(['en', 'fr', 'de', 'es']);
  });

  it('changing the selection updates the active language and persists it to localStorage', () => {
    const useSpy = jest.spyOn(translate, 'use');
    const select: HTMLSelectElement = fixture.debugElement.query(By.css('select')).nativeElement;

    select.value = 'fr';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(useSpy).toHaveBeenCalledWith('fr');
    expect(translate.currentLang).toBe('fr');
    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe('fr');
  });

  it('ignores a change to an unsupported language code', () => {
    const useSpy = jest.spyOn(translate, 'use');

    component.onLanguageChange({ target: { value: 'xx' } } as unknown as Event);

    expect(useSpy).not.toHaveBeenCalled();
    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBeNull();
  });
});
