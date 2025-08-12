import { PhoneNumberUtil } from 'google-libphonenumber';

export class PhoneNumberService {
  private static phoneUtil = PhoneNumberUtil.getInstance();

  static validatePhoneNumber(phoneNumber: string, regionCode: string): boolean {
    try {
      const parsedNumber = this.phoneUtil.parse(phoneNumber, regionCode);
      return this.phoneUtil.isValidNumber(parsedNumber);
    } catch {
      return false;
    }
  }

  static validatePhoneNumberWithDetails(phoneNumber: string, regionCode: string): {
    isValid: boolean;
    errorMessage?: string;
  } {
    try {
      const parsedNumber = this.phoneUtil.parse(phoneNumber, regionCode);
      const isValid = this.phoneUtil.isValidNumber(parsedNumber);

      if (isValid) {
        return { isValid: true };
      }

      // Check specific validation types
      const isPossible = this.phoneUtil.isPossibleNumber(parsedNumber);
      if (!isPossible) {
        return {
          isValid: false,
          errorMessage: 'Phone number has incorrect length or format'
        };
      }

      return {
        isValid: false,
        errorMessage: 'Phone number format is not valid for this country'
      };
    } catch {
      // Parse error usually means invalid format
      return {
        isValid: false,
        errorMessage: 'Invalid phone number format'
      };
    }
  }

  static formatPhoneNumber(phoneNumber: string, regionCode: string): string {
    try {
      const parsedNumber = this.phoneUtil.parse(phoneNumber, regionCode);
      // 1 corresponds to PhoneNumberFormat.E164
      const formatted = this.phoneUtil.format(parsedNumber, 1);
      // Ensure no spaces in the final output
      return formatted.replace(/\s+/g, '');
    } catch {
      return phoneNumber;
    }
  }

  static getCountryCodeForRegion(regionCode: string): number {
    return this.phoneUtil.getCountryCodeForRegion(regionCode);
  }

  static parseAndValidatePhoneNumber(phoneNumberWithCountryCode: string): {
    isValid: boolean;
    formattedNumber?: string;
    regionCode?: string;
  } {
    try {
      const parsedNumber = this.phoneUtil.parseAndKeepRawInput(
        phoneNumberWithCountryCode,
        undefined,
      );
      const isValid = this.phoneUtil.isValidNumber(parsedNumber);

      if (isValid) {
        const formatted = this.phoneUtil.format(parsedNumber, 1);
        return {
          isValid: true,
          // 1 corresponds to PhoneNumberFormat.E164
          formattedNumber: formatted.replace(/\s+/g, ''),
          regionCode:
            this.phoneUtil.getRegionCodeForNumber(parsedNumber) || undefined,
        };
      }

      return { isValid: false };
    } catch {
      return { isValid: false };
    }
  }
}
