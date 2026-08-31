const CURRENCY_CODE_PATTERN = /^[A-Za-z]{3}$/;

export function isValidCurrencyCode(value: string): boolean {
  return CURRENCY_CODE_PATTERN.test(value.trim());
}
