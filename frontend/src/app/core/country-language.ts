export const COUNTRY_STORAGE_KEY = 'carelink-country';

// Maps an ISO 3166-1 alpha-2 country code (from the browser's locale, e.g.
// the "IN" in "en-IN") to the region name stored on our Tenant records.
// Only entries that match an actual tenant's Region field ever take effect -
// this list is intentionally broader than today's two tenants so it needs no
// changes as more hospitals/regions are added later.
export const COUNTRY_CODE_TO_REGION: Record<string, string> = {
  IN: 'India',
  DE: 'Germany',
  US: 'United States',
  GB: 'United Kingdom',
  CA: 'Canada',
  AU: 'Australia',
  FR: 'France',
  ES: 'Spain',
  MX: 'Mexico',
  AR: 'Argentina',
  AT: 'Austria',
  CH: 'Switzerland',
  BE: 'Belgium',
  IT: 'Italy',
  BR: 'Brazil',
  PT: 'Portugal',
  NL: 'Netherlands'
};

/**
 * Attempts to map a BCP-47 locale string (e.g. "en-IN", "de-DE") to one of the
 * given available regions, via the country subtag. Returns null when the
 * locale has no country subtag, or when it doesn't map to any known/available
 * region - callers should fall back to a sensible default in that case.
 */
export function detectRegionFromLocale(locale: string, availableRegions: string[]): string | null {
  if (!locale) {
    return null;
  }

  const parts = locale.split('-');
  if (parts.length < 2) {
    return null;
  }

  const countryCode = parts[1].toUpperCase();
  const region = COUNTRY_CODE_TO_REGION[countryCode];

  return region && availableRegions.includes(region) ? region : null;
}
