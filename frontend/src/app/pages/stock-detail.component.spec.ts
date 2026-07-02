import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, throwError } from 'rxjs';
import { AiExplanationResponse } from '../core/models';
import { PredictionHistoryService } from '../core/prediction-history.service';
import { StockApiService } from '../core/stock-api.service';
import { WatchlistService } from '../core/watchlist.service';
import { StockDetailComponent } from './stock-detail.component';

describe('StockDetailComponent AI explanation', () => {
  let response$: Subject<AiExplanationResponse>;
  let component: StockDetailComponent;
  let api: { generateAiExplanation: jasmine.Spy };

  beforeEach(() => {
    response$ = new Subject<AiExplanationResponse>();
    api = { generateAiExplanation: jasmine.createSpy('generateAiExplanation').and.returnValue(response$) };
    TestBed.configureTestingModule({
      imports: [StockDetailComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { paramMap: new Subject() } },
        { provide: Router, useValue: { navigate: jasmine.createSpy('navigate') } },
        { provide: StockApiService, useValue: api },
        { provide: PredictionHistoryService, useValue: { add: () => undefined } },
        { provide: WatchlistService, useValue: { has: () => false, toggle: () => undefined } },
      ],
    }).overrideComponent(StockDetailComponent, { set: { template: '' } });
    component = TestBed.createComponent(StockDetailComponent).componentInstance;
  });

  it('shows loading until a successful explanation arrives', () => {
    component.generateAiExplanation(false);
    expect(component.aiLoading()).toBeTrue();

    response$.next(aiResponse(false));
    response$.complete();

    expect(component.aiLoading()).toBeFalse();
    expect(component.aiExplanation()?.provider).toBe('openai');
  });

  it('keeps deterministic fallback metadata visible', () => {
    component.generateAiExplanation(false);
    response$.next(aiResponse(true));
    response$.complete();

    expect(component.aiExplanation()?.fallbackUsed).toBeTrue();
    expect(component.aiExplanation()?.provider).toBe('deterministic');
  });

  it('shows quota state when AI explanation limit is reached', () => {
    api.generateAiExplanation.and.returnValue(throwError(() => ({
      status: 402,
      error: { detail: 'This plan limit has been reached. Upgrade to Pro for higher limits.' },
    })));

    component.generateAiExplanation(false);

    expect(component.aiLoading()).toBeFalse();
    expect(component.aiQuotaExceeded()).toBeTrue();
    expect(component.aiError()).toContain('Upgrade to Pro');
  });

  it('clears quota state before retrying an explanation', () => {
    component.aiQuotaExceeded.set(true);
    component.aiError.set('Limit reached.');

    component.generateAiExplanation(false);

    expect(component.aiQuotaExceeded()).toBeFalse();
    expect(component.aiError()).toBe('');
  });

  function aiResponse(fallbackUsed: boolean): AiExplanationResponse {
    return {
      ticker: 'AAPL', provider: fallbackUsed ? 'deterministic' : 'openai', model: fallbackUsed ? 'deterministic-v1' : 'gpt-5.5',
      fallbackUsed, fallbackReason: fallbackUsed ? 'timeout' : null, generatedAt: new Date().toISOString(), cached: false,
      explanation: { ticker: 'AAPL', prediction: 'UP', confidence: 72, summary: 'Summary', supporting_signals: [], conflicting_signals: [], risk_level: 'MEDIUM', risk_factors: [], what_could_change_the_view: [], beginner_explanation: 'Simple', data_limitations: [], disclaimer: 'Educational purpose only. Not financial advice.' },
    };
  }
});
