import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { WatchlistService } from './watchlist.service';
import { StockApiService } from './stock-api.service';
import { of, Subject, throwError } from 'rxjs';

describe('WatchlistService', () => {
  beforeEach(() => {
    localStorage.clear(); TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [{ provide: StockApiService, useValue: { getWorkspaceWatchlist: () => of([]), saveWorkspaceWatchlist: (items: unknown) => of(items), recordMonetizationEvent: () => of({ eventName: 'test', message: 'ok' }) } }] });
  });

  it('migrates the legacy ticker list into structured items', () => {
    localStorage.setItem('stock-analyzer-watchlist-v1', JSON.stringify(['aapl']));
    const service = TestBed.inject(WatchlistService);
    expect(service.items()[0]).toEqual(jasmine.objectContaining({ ticker: 'AAPL', note: '', tags: [] }));
  });

  it('normalizes, deduplicates, and limits tags', () => {
    const service = TestBed.inject(WatchlistService);
    service.updateDetails('AAPL', '  Watch earnings reaction.  ', ['Growth', 'growth', ' Mega Cap ', 'tech', 'us', 'extra']);
    expect(service.get('AAPL')).toEqual(jasmine.objectContaining({
      note: 'Watch earnings reaction.',
      tags: ['growth', 'mega cap', 'tech', 'us', 'extra'],
    }));
  });

  it('debounces saves and sends only the latest watchlist state', fakeAsync(() => {
    const api = TestBed.inject(StockApiService) as jasmine.SpyObj<StockApiService>;
    api.saveWorkspaceWatchlist = jasmine.createSpy('saveWorkspaceWatchlist').and.callFake((items) => of(items));
    const service = TestBed.inject(WatchlistService);

    service.toggle('TSLA');
    service.toggle('NVDA');
    tick(149);
    expect(api.saveWorkspaceWatchlist).not.toHaveBeenCalled();

    tick(1);
    expect(api.saveWorkspaceWatchlist).toHaveBeenCalledTimes(1);
    expect(api.saveWorkspaceWatchlist).toHaveBeenCalledWith(service.items());
  }));

  it('rolls back optimistic additions when the backend reports a watchlist quota limit', fakeAsync(() => {
    const api = TestBed.inject(StockApiService) as jasmine.SpyObj<StockApiService>;
    api.saveWorkspaceWatchlist = jasmine.createSpy('saveWorkspaceWatchlist').and.returnValue(throwError(() => ({
      status: 402,
      error: { detail: 'This plan limit has been reached. Upgrade to Pro for higher limits.' },
    })));
    const service = TestBed.inject(WatchlistService);

    service.toggle('TSLA');
    tick(150);

    expect(service.has('TSLA')).toBeFalse();
    expect(service.quotaExceeded()).toBeTrue();
    expect(service.quotaMessage()).toContain('Upgrade to Pro');
    expect(service.syncState()).toBe('blocked');
  }));

  it('does not let late hydration overwrite local edits', fakeAsync(() => {
    const remote = new Subject<unknown[]>();
    TestBed.resetTestingModule();
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [{
        provide: StockApiService,
        useValue: {
          getWorkspaceWatchlist: () => remote.asObservable(),
          saveWorkspaceWatchlist: (items: unknown) => of(items),
          recordMonetizationEvent: () => of({ eventName: 'test', message: 'ok' }),
        },
      }],
    });
    const service = TestBed.inject(WatchlistService);

    service.updateDetails('AAPL', 'Local note', ['local']);
    remote.next([{ ticker: 'TSLA', addedAt: new Date().toISOString(), note: 'Remote', tags: [] }]);
    tick(150);

    expect(service.get('AAPL')?.note).toBe('Local note');
    expect(service.has('TSLA')).toBeFalse();
  }));
});
