import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { finalize, map, switchMap } from 'rxjs/operators';
import { HistoricalPrice, IndicatorResponse, MlPrediction } from '../core/models';
import { PredictionHistoryService } from '../core/prediction-history.service';
import { StockApiService } from '../core/stock-api.service';
import { PriceChartComponent } from '../shared/price-chart.component';
import { normalizeTicker, tickerValidationMessage } from '../core/ticker-validation';

@Component({
  imports: [CommonModule, FormsModule, PriceChartComponent],
  template: `
    <main class="page">
      <form class="top-search" (ngSubmit)="search()" novalidate><input name="ticker" [(ngModel)]="ticker" (ngModelChange)="validationError.set('')" aria-label="Ticker" maxlength="20" autocomplete="off" /><button type="submit" [disabled]="loading() || retraining()">{{ loading() ? 'Analyzing...' : 'Analyze' }}</button></form>
      @if (validationError()) { <div class="error card" role="alert">{{ validationError() }}</div> }
      @if (loading()) {
        <div class="loading card" role="status" aria-live="polite">
          <span class="spinner" aria-hidden="true"></span>
          <strong>{{ loadingMessage() }}</strong>
          <p>First-time analysis may take longer because the model is trained for this ticker.</p>
        </div>
      }
      @if (error()) { <div class="error card" role="alert"><p>{{ error() }}</p><button type="button" class="retry-button" (click)="load()">Try again</button></div> }
      @if (prediction(); as result) {
        <div class="page-heading"><div><p class="eyebrow">STOCK DETAIL</p><h1>{{ result.ticker }}</h1><p class="muted">Latest market analysis</p></div><div class="latest-price"><span>Latest close</span><strong>{{ result.latest_close | number:'1.2-2' }}</strong></div></div>
        @if (result.model_trained) { <div class="notice" role="status">A new Random Forest model was trained and saved for {{ result.ticker }}.</div> }
        @if (result.fallback_used) { <div class="notice warning" role="status">There was not enough history to train a reliable ML model, so this result uses the rule-based fallback.</div> }
        <div class="notice warning">First-time analysis may take longer because the model is trained for this ticker.</div>
        <div class="notice model-summary">
          <span class="badge model-badge" [class.new-model]="result.model_status === 'newly_trained_model'" [class.fallback]="result.model_status === 'rule_based_fallback'">{{ modelStatusLabel(result.model_status) }}</span>
          @if (result.model_accuracy !== null) { <span>Accuracy {{ result.model_accuracy | percent:'1.0-1' }}</span> }
          <span>{{ result.warning }}</span>
          <button type="button" class="secondary-button retrain-button" [disabled]="retraining()" (click)="retrainModel()">{{ retraining() ? 'Retraining...' : 'Retrain Model' }}</button>
        </div>
        @if (retraining()) { <div class="training-progress" role="status" aria-live="polite"><span class="spinner" aria-hidden="true"></span> Training model with the latest 5 years of data. This may take a moment...</div> }
        @if (retrainSuccess()) { <div class="success-message" role="status">{{ retrainSuccess() }}</div> }
        @if (retrainError()) { <div class="error-message" role="alert">{{ retrainError() }}</div> }
        <section class="summary-grid">
          <article class="prediction-card card" [class.down]="result.prediction === 'DOWN'"><span>Tomorrow's signal</span><strong>{{ result.prediction }}</strong><p>{{ result.confidence }}% confidence</p><div class="meter"><i [style.width.%]="result.confidence"></i></div></article>
          <article class="card"><span>Probability UP</span><strong>{{ result.probability_up | percent:'1.0-1' }}</strong><p>Model-estimated chance</p></article>
          <article class="card"><span>Probability DOWN</span><strong>{{ result.probability_down | percent:'1.0-1' }}</strong><p>Model-estimated chance</p></article>
        </section>
        <section class="content-grid">
          <article class="card chart-card"><div class="section-title"><h2>Closing price</h2><span>1 year</span></div><app-price-chart [prices]="history()" /></article>
          <article class="card"><h2>Why this prediction?</h2><ul class="reasons">@for (reason of result.reasons; track reason) { <li>{{ reason }}</li> }</ul></article>
        </section>
        @if (indicators(); as data) {
          <section class="card indicators"><div class="section-title"><h2>Technical indicators</h2><span>{{ data.latest.date }}</span></div>
            <div class="indicator-grid">
              <div><span>RSI 14</span><strong>{{ data.latest.RSI_14 | number:'1.2-2' }}</strong></div><div><span>SMA 20</span><strong>{{ data.latest.SMA_20 | number:'1.2-2' }}</strong></div><div><span>SMA 50</span><strong>{{ data.latest.SMA_50 | number:'1.2-2' }}</strong></div><div><span>EMA 20</span><strong>{{ data.latest.EMA_20 | number:'1.2-2' }}</strong></div><div><span>EMA 50</span><strong>{{ data.latest.EMA_50 | number:'1.2-2' }}</strong></div><div><span>MACD</span><strong>{{ data.latest.MACD | number:'1.2-2' }}</strong></div><div><span>MACD signal</span><strong>{{ data.latest.MACD_signal | number:'1.2-2' }}</strong></div><div><span>Volume change</span><strong>{{ data.latest.volume_change | percent:'1.0-1' }}</strong></div>
            </div>
          </section>
        }
      }
    </main>`,
})
export class StockDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute); private readonly router = inject(Router); private readonly api = inject(StockApiService); private readonly historyStore = inject(PredictionHistoryService);
  private trainingMessageTimer: ReturnType<typeof setTimeout> | undefined;
  ticker = 'RELIANCE.NS'; readonly loading = signal(false); readonly retraining = signal(false); readonly retrainSuccess = signal(''); readonly retrainError = signal(''); readonly loadingMessage = signal('Checking model for this stock...'); readonly error = signal(''); readonly validationError = signal(''); readonly history = signal<HistoricalPrice[]>([]); readonly indicators = signal<IndicatorResponse | null>(null); readonly prediction = signal<MlPrediction | null>(null);
  ngOnInit(): void { this.route.paramMap.subscribe((params) => { this.ticker = params.get('ticker') ?? 'RELIANCE.NS'; this.load(); }); }
  ngOnDestroy(): void { this.clearTrainingMessageTimer(); }
  search(): void { this.validationError.set(tickerValidationMessage(this.ticker)); if (!this.validationError()) this.router.navigate(['/stocks', normalizeTicker(this.ticker)]); }
  load(): void {
    this.clearTrainingMessageTimer(); this.loading.set(true); this.loadingMessage.set('Checking model for this stock...'); this.error.set(''); this.prediction.set(null);
    this.trainingMessageTimer = setTimeout(() => this.loadingMessage.set('Training model for this stock for the first time...'), 1800);
    forkJoin({ history: this.api.getHistory(this.ticker), indicators: this.api.getIndicators(this.ticker), prediction: this.api.getMlPrediction(this.ticker) }).subscribe({
      next: ({ history, indicators, prediction }) => {
        this.clearTrainingMessageTimer();
        if (prediction.model_status === 'newly_trained_model') {
          this.loadingMessage.set('Training model for this stock for the first time...');
          this.trainingMessageTimer = setTimeout(() => this.showResult(history, indicators, prediction), 700);
          return;
        }
        this.showResult(history, indicators, prediction);
      },
      error: (error) => { this.clearTrainingMessageTimer(); this.loading.set(false); this.error.set(error.error?.detail ?? 'We could not load this stock. Check the ticker and try again.'); },
    });
  }
  modelStatusLabel(status: MlPrediction['model_status']): string { return status === 'existing_model' ? 'Existing ML Model' : status === 'newly_trained_model' ? 'Newly Trained ML Model' : 'Rule-Based Fallback'; }
  retrainModel(): void {
    if (this.retraining()) return;
    this.retraining.set(true); this.retrainSuccess.set(''); this.retrainError.set('');
    this.api.retrainModel(this.ticker).pipe(
      switchMap((training) => this.api.getMlPrediction(this.ticker).pipe(
        map((prediction) => ({ training, prediction })),
      )),
      finalize(() => this.retraining.set(false)),
    ).subscribe({
      next: ({ training, prediction }) => {
        this.prediction.set(prediction); this.historyStore.add(prediction);
        this.retrainSuccess.set(`Model retrained successfully. Accuracy ${(training.accuracy * 100).toFixed(1)}%, precision ${(training.precision * 100).toFixed(1)}%, recall ${(training.recall * 100).toFixed(1)}%. Prediction refreshed.`);
      },
      error: (error) => this.retrainError.set(error.error?.detail ?? 'The model could not be retrained. Please try again.'),
    });
  }
  private showResult(history: HistoricalPrice[], indicators: IndicatorResponse, prediction: MlPrediction): void { this.clearTrainingMessageTimer(); this.history.set(history); this.indicators.set(indicators); this.prediction.set(prediction); this.historyStore.add(prediction); this.loading.set(false); }
  private clearTrainingMessageTimer(): void { if (this.trainingMessageTimer !== undefined) { clearTimeout(this.trainingMessageTimer); this.trainingMessageTimer = undefined; } }
}
