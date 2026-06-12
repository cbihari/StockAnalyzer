import { TestBed } from '@angular/core/testing';
import { WatchlistService } from './watchlist.service';

describe('WatchlistService', () => {
  beforeEach(() => { localStorage.clear(); TestBed.resetTestingModule(); });

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
});
