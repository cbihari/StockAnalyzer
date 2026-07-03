import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { PortfolioService } from './portfolio.service';
import { StockApiService } from './stock-api.service';

describe('PortfolioService', () => {
  beforeEach(() => {
    localStorage.clear(); TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [{ provide: StockApiService, useValue: { getWorkspacePortfolio: () => of([]), saveWorkspacePortfolio: (items: unknown) => of(items), recordMonetizationEvent: () => of({ eventName: 'test', message: 'ok' }) } }] });
  });

  it('adds a normalized holding and persists it', () => {
    const service = TestBed.inject(PortfolioService);
    service.add({ ticker: ' aapl ', quantity: 2.5, averageCost: 150, purchasedAt: '2026-06-01', note: '  Long term  ' });

    expect(service.holdings()[0]).toEqual(jasmine.objectContaining({ ticker: 'AAPL', quantity: 2.5, average_cost: 150, note: 'Long term' }));
    expect(JSON.parse(localStorage.getItem('stock-analyzer-portfolio-v1') ?? '[]').length).toBe(1);
  });

  it('removes a holding by id', () => {
    const service = TestBed.inject(PortfolioService);
    service.add({ ticker: 'MSFT', quantity: 1, averageCost: 300, purchasedAt: '', note: '' });
    service.remove(service.holdings()[0].id);
    expect(service.holdings()).toEqual([]);
  });

  it('ignores stale save responses so newer local state is not overwritten', fakeAsync(() => {
    const firstSave = new Subject<unknown[]>();
    const secondSave = new Subject<unknown[]>();
    let saveCalls = 0;
    TestBed.resetTestingModule();
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [{
        provide: StockApiService,
        useValue: {
          getWorkspacePortfolio: () => of([]),
          saveWorkspacePortfolio: () => (++saveCalls === 1 ? firstSave : secondSave).asObservable(),
          recordMonetizationEvent: () => of({ eventName: 'test', message: 'ok' }),
        },
      }],
    });
    const service = TestBed.inject(PortfolioService);

    service.add({ ticker: 'AAPL', quantity: 1, averageCost: 150, purchasedAt: '', note: 'first' });
    tick(150);
    service.add({ ticker: 'MSFT', quantity: 1, averageCost: 300, purchasedAt: '', note: 'second' });
    tick(150);

    firstSave.next([{ id: 'stale', ticker: 'TSLA', quantity: 1, average_cost: 1, purchased_at: null, note: 'stale' }]);
    expect(service.holdings().map((holding) => holding.ticker)).toEqual(['AAPL', 'MSFT']);

    secondSave.next(service.holdings());
    expect(service.syncState()).toBe('synced');
  }));

  it('rolls back optimistic holdings when the backend reports a portfolio quota limit', fakeAsync(() => {
    const api = TestBed.inject(StockApiService) as jasmine.SpyObj<StockApiService>;
    api.saveWorkspacePortfolio = jasmine.createSpy('saveWorkspacePortfolio').and.returnValue(throwError(() => ({
      status: 402,
      error: { detail: 'This plan limit has been reached. Upgrade to Pro for higher limits.' },
    })));
    const service = TestBed.inject(PortfolioService);

    service.add({ ticker: 'AAPL', quantity: 1, averageCost: 150, purchasedAt: '', note: '' });
    tick(150);

    expect(service.holdings()).toEqual([]);
    expect(service.quotaExceeded()).toBeTrue();
    expect(service.quotaMessage()).toContain('Upgrade to Pro');
    expect(service.syncState()).toBe('blocked');
  }));
});
