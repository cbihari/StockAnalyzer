import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { AlertService } from './alert.service';
import { MarketInstrument } from './models';
import { StockApiService } from './stock-api.service';
import { of, throwError } from 'rxjs';

const quote: MarketInstrument = { symbol: 'AAPL', name: 'Apple', price: 200, change: 4, change_percent: .02, day_high: 201, day_low: 194, volume: 1, sparkline: [] };

describe('AlertService', () => {
  beforeEach(() => {
    localStorage.clear(); TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [{ provide: StockApiService, useValue: { getWorkspaceAlerts: () => of({ rules: [], notifications: [] }), saveWorkspaceAlerts: (state: unknown) => of(state), recordMonetizationEvent: () => of({ eventName: 'test', message: 'ok' }) } }] });
  });

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

  it('debounces alert saves to the latest state', fakeAsync(() => {
    const api = TestBed.inject(StockApiService) as jasmine.SpyObj<StockApiService>;
    api.saveWorkspaceAlerts = jasmine.createSpy('saveWorkspaceAlerts').and.callFake((state) => of(state));
    const service = TestBed.inject(AlertService);

    const rule = service.add('AAPL', { type: 'price_above', threshold: 190, frequency: 'daily', cooldownHours: 24, quietStart: '', quietEnd: '' });
    service.toggle(rule.id);
    tick(150);

    expect(api.saveWorkspaceAlerts).toHaveBeenCalledTimes(1);
    expect(api.saveWorkspaceAlerts).toHaveBeenCalledWith({ rules: service.rules(), notifications: service.notifications() });
  }));

  it('rolls back optimistic alert rules when the backend reports an alert quota limit', fakeAsync(() => {
    const api = TestBed.inject(StockApiService) as jasmine.SpyObj<StockApiService>;
    api.saveWorkspaceAlerts = jasmine.createSpy('saveWorkspaceAlerts').and.returnValue(throwError(() => ({
      status: 402,
      error: { detail: 'This plan limit has been reached. Upgrade to Pro for higher limits.' },
    })));
    const service = TestBed.inject(AlertService);

    service.add('AAPL', { type: 'price_above', threshold: 190, frequency: 'daily', cooldownHours: 24, quietStart: '', quietEnd: '' });
    tick(150);

    expect(service.rules()).toEqual([]);
    expect(service.quotaExceeded()).toBeTrue();
    expect(service.quotaMessage()).toContain('Upgrade to Pro');
    expect(service.syncState()).toBe('blocked');
  }));
});
