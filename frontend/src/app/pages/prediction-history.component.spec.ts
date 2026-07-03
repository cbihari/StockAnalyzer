import { HttpHeaders, HttpResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { StockApiService } from '../core/stock-api.service';
import { PredictionHistoryComponent } from './prediction-history.component';

describe('PredictionHistoryComponent CSV export', () => {
  let component: PredictionHistoryComponent;
  let api: {
    getPredictionHistory: jasmine.Spy;
    exportPredictionHistoryCsv: jasmine.Spy;
    evaluatePredictions: jasmine.Spy;
    recordMonetizationEvent: jasmine.Spy;
  };

  beforeEach(() => {
    api = {
      getPredictionHistory: jasmine.createSpy('getPredictionHistory').and.returnValue(of({
        items: [],
        total: 0,
        evaluated: 0,
        pending: 0,
        correct: 0,
        wrong: 0,
        accuracy_percentage: 0,
      })),
      exportPredictionHistoryCsv: jasmine.createSpy('exportPredictionHistoryCsv'),
      evaluatePredictions: jasmine.createSpy('evaluatePredictions'),
      recordMonetizationEvent: jasmine.createSpy('recordMonetizationEvent').and.returnValue(of({ eventName: 'test', message: 'ok' })),
    };
    TestBed.configureTestingModule({
      imports: [PredictionHistoryComponent],
      providers: [{ provide: StockApiService, useValue: api }],
    }).overrideComponent(PredictionHistoryComponent, { set: { template: '' } });
    component = TestBed.createComponent(PredictionHistoryComponent).componentInstance;
  });

  it('downloads the backend CSV export', () => {
    const click = jasmine.createSpy('click');
    spyOn(document, 'createElement').and.returnValue({ click, href: '', download: '' } as unknown as HTMLAnchorElement);
    spyOn(URL, 'createObjectURL').and.returnValue('blob:test');
    spyOn(URL, 'revokeObjectURL');
    api.exportPredictionHistoryCsv.and.returnValue(of(new HttpResponse({
      body: new Blob(['csv'], { type: 'text/csv;charset=utf-8' }),
      headers: new HttpHeaders({
        'content-disposition': 'attachment; filename="stockanalyzer-predictions-test.csv"',
      }),
    })));

    component.exportCsv();

    expect(api.exportPredictionHistoryCsv).toHaveBeenCalledWith('', 'all');
    expect(click).toHaveBeenCalled();
    expect(component.exportMessage()).toBe('Prediction history CSV exported.');
    expect(component.exportQuotaExceeded()).toBeFalse();
  });

  it('shows upgrade state when CSV export quota is exhausted', () => {
    api.exportPredictionHistoryCsv.and.returnValue(throwError(() => ({
      status: 402,
      error: { detail: 'This plan limit has been reached. Upgrade to Pro for higher limits.' },
    })));

    component.exportCsv();

    expect(component.exporting()).toBeFalse();
    expect(component.exportQuotaExceeded()).toBeTrue();
    expect(component.exportMessage()).toContain('Upgrade to Pro');
    expect(api.recordMonetizationEvent).toHaveBeenCalledWith(jasmine.objectContaining({
      eventName: 'paid_feature_attempt',
      featureKey: 'csv_export',
    }));
  });
});
