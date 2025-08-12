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

  static formatPhoneNumber(phoneNumber: string, regionCode: string): string {
    try {
      const parsedNumber = this.phoneUtil.parse(phoneNumber, regionCode);
      // 1 corresponds to PhoneNumberFormat.E164
      return this.phoneUtil.format(parsedNumber, 1);
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
        return {
          isValid: true,
          // 1 corresponds to PhoneNumberFormat.E164
          formattedNumber: this.phoneUtil.format(parsedNumber, 1),
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
