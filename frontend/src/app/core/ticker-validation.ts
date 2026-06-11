export const TICKER_PATTERN = /^[A-Z0-9.^=\-]{1,20}$/;

export function normalizeTicker(value: string): string {
  return value.trim().toUpperCase();
}

export function tickerValidationMessage(value: string): string {
  const ticker = normalizeTicker(value);
  if (!ticker) {
    return 'Enter a ticker symbol.';
  }

  return TICKER_PATTERN.test(ticker)
    ? ''
    : 'Use 1-20 letters, numbers, dots, carets, equals signs, or hyphens.';
}
