import { detectRegionFromLocale } from './country-language';

describe('detectRegionFromLocale', () => {
  const availableRegions = ['India', 'Germany'];

  it('maps a locale with a known country subtag to its region', () => {
    expect(detectRegionFromLocale('en-IN', availableRegions)).toBe('India');
    expect(detectRegionFromLocale('de-DE', availableRegions)).toBe('Germany');
  });

  it('is case-insensitive on the country subtag', () => {
    expect(detectRegionFromLocale('en-in', availableRegions)).toBe('India');
  });

  it('returns null when the mapped region is not in the available list', () => {
    // FR maps to "France" in the table, but it is not an available tenant region here.
    expect(detectRegionFromLocale('fr-FR', availableRegions)).toBeNull();
  });

  it('returns null when the locale has no country subtag', () => {
    expect(detectRegionFromLocale('en', availableRegions)).toBeNull();
  });

  it('returns null when the locale is empty', () => {
    expect(detectRegionFromLocale('', availableRegions)).toBeNull();
  });

  it('returns null for an unrecognized country code', () => {
    expect(detectRegionFromLocale('en-ZZ', availableRegions)).toBeNull();
  });
});
