/**
 * Date utility functions for the application
 */

/**
 * Calculates and formats the age from a date of birth string
 * @param dateOfBirth - The date of birth as an ISO string (YYYY-MM-DD)
 * @returns A formatted age string (e.g., "2 years 3 months", "8 months", "Unknown age")
 */
export function getAgeLabel(dateOfBirth: string | undefined): string {
  if (!dateOfBirth) return 'Unknown age';

  const birthDate = new Date(dateOfBirth);
  const today = new Date();

  let years = today.getFullYear() - birthDate.getFullYear();
  let months = today.getMonth() - birthDate.getMonth();

  // Adjust if the current day is before the birth day in the month
  if (today.getDate() < birthDate.getDate()) {
    months--;
  }

  // Adjust if months is negative
  if (months < 0) {
    years--;
    months += 12;
  }

  // Format the output
  if (years === 0 && months === 0) {
    return 'Less than 1 month';
  } else if (years === 0) {
    return months === 1 ? '1 month' : `${months} months`;
  } else if (months === 0) {
    return years === 1 ? '1 year' : `${years} years`;
  } else {
    const yearText = years === 1 ? '1 year' : `${years} years`;
    const monthText = months === 1 ? '1 month' : `${months} months`;
    return `${yearText} ${monthText}`;
  }
}

/**
 * Formats a Date object to a local date string (YYYY-MM-DD) without timezone conversion
 * @param date - The Date object to format
 * @returns A date string in YYYY-MM-DD format, or empty string if date is null
 */
export function formatDateToLocalString(date: Date | null): string {
  if (!date) return '';

  // Format date in local timezone as YYYY-MM-DD
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}
