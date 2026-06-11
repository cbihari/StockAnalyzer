import { normalizeTicker, tickerValidationMessage } from './ticker-validation';

describe('ticker validation', () => {
  it('normalizes whitespace and casing', () => {
    expect(normalizeTicker(' reliance.ns ')).toBe('RELIANCE.NS');
  });

  it('accepts common Yahoo Finance symbols', () => {
    expect(tickerValidationMessage('RELIANCE.NS')).toBe('');
    expect(tickerValidationMessage('^NSEI')).toBe('');
  });

  it('rejects blank and unsafe symbols', () => {
    expect(tickerValidationMessage('')).toBe('Enter a ticker symbol.');
    expect(tickerValidationMessage('AAPL/USD')).not.toBe('');
  });
});
