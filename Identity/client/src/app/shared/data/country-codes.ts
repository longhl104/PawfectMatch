import { PhoneNumberUtil } from 'google-libphonenumber';

export interface CountryCode {
  label: string;
  value: string;
  code: string;
  regionCode: string;
}

// Build full list dynamically from libphonenumber
const phoneUtil = PhoneNumberUtil.getInstance();

function buildCountryCodes(): CountryCode[] {
  // Map of calling code (number) -> list of region codes (e.g., 1 -> ['US','CA','BS', ...])
  const codeToRegions = new Map<number, string[]>();

  const regions = Array.from(phoneUtil.getSupportedRegions()); // e.g., ['US','GB', ...]
  for (const region of regions) {
    const calling = phoneUtil.getCountryCodeForRegion(region);
    if (!calling) continue;
    const list = codeToRegions.get(calling) ?? [];
    list.push(region);
    codeToRegions.set(calling, list);
  }

  // Build CountryCode entries grouped by calling code, sort regions alphabetically
  const entries: CountryCode[] = [];
  for (const [calling, regionList] of codeToRegions.entries()) {
    const sorted = regionList.sort();
    const value = `+${calling}`;
    const label = `${value} (${sorted.join('/')})`;
    // Keep a primary region for compatibility with existing interface
    const primary = sorted[0] ?? '';
    entries.push({ label, value, code: primary, regionCode: primary });
  }

  // Sort by calling code ascending (numeric), then label
  entries.sort((a, b) => {
    const an = parseInt(a.value.replace('+', ''), 10);
    const bn = parseInt(b.value.replace('+', ''), 10);
    if (an !== bn) return an - bn;
    return a.label.localeCompare(b.label);
  });

  return entries;
}

export const COUNTRY_CODES: CountryCode[] = buildCountryCodes();
