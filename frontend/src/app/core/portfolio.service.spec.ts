import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { PortfolioService } from './portfolio.service';
import { StockApiService } from './stock-api.service';

describe('PortfolioService', () => {
  beforeEach(() => {
    localStorage.clear(); TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [{ provide: StockApiService, useValue: { getWorkspacePortfolio: () => of([]), saveWorkspacePortfolio: (items: unknown) => of(items) } }] });
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
});
