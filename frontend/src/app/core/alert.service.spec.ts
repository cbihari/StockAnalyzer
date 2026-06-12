import { TestBed } from '@angular/core/testing';
import { AlertService } from './alert.service';
import { MarketInstrument } from './models';

const quote: MarketInstrument = { symbol: 'AAPL', name: 'Apple', price: 200, change: 4, change_percent: .02, day_high: 201, day_low: 194, volume: 1, sparkline: [] };

describe('AlertService', () => {
  beforeEach(() => { localStorage.clear(); TestBed.resetTestingModule(); });

  it('triggers a matching price rule and disables once-only alerts', () => {
    const service = TestBed.inject(AlertService);
    const rule = service.add('aapl', { type: 'price_above', threshold: 190, frequency: 'once', cooldownHours: 24, quietStart: '', quietEnd: '' });
    const triggered = service.evaluate([quote], '2026-06-12T10:00:00Z', new Date('2026-06-12T10:00:00Z'));
    expect(triggered.length).toBe(1);
    expect(service.rules().find((item) => item.id === rule.id)?.enabled).toBeFalse();
    expect(service.unreadCount()).toBe(1);
  });

  it('does not trigger during configured quiet hours', () => {
    const service = TestBed.inject(AlertService);
    service.add('AAPL', { type: 'daily_move', threshold: 1, frequency: 'daily', cooldownHours: 24, quietStart: '22:00', quietEnd: '07:00' });
    expect(service.evaluate([quote], '2026-06-12T23:00:00Z', new Date('2026-06-12T23:00:00Z'))).toEqual([]);
  });
});
