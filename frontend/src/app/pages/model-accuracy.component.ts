import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { StockApiService } from '../core/stock-api.service';
import { TickerModelMetrics } from '../core/models';
import { normalizeTicker, tickerValidationMessage } from '../core/ticker-validation';

@Component({
  imports: [CommonModule, FormsModule],
  template: `
    <main class="page">
      <p class="eyebrow">MODEL ACCURACY</p>
      <h1>Model performance by ticker</h1>
      <p class="lead">Review the latest chronological holdout results for any trained ticker-specific Random Forest model.</p>

      <form class="accuracy-search card" (ngSubmit)="loadMetrics()" novalidate>
        <label for="accuracy-ticker">Ticker symbol</label>
        <div class="search-row">
          <input id="accuracy-ticker" name="ticker" [(ngModel)]="ticker" (ngModelChange)="validationError.set('')" maxlength="20" autocomplete="off" placeholder="RELIANCE.NS" />
          <button type="submit" [disabled]="loading()">{{ loading() ? 'Loading...' : 'View metrics' }}</button>
        </div>
        @if (validationError()) { <p class="field-error" role="alert">{{ validationError() }}</p> }
      </form>

      @if (loading()) { <div class="loading card" role="status"><span class="spinner" aria-hidden="true"></span> Loading model metrics...</div> }
      @if (error()) { <div class="empty card" role="alert"><h2>{{ error() }}</h2><p>Try another ticker or open Stock Detail to create its model.</p></div> }

      @if (metrics(); as model) {
        <div class="model-heading">
          <div><span class="badge model-badge">{{ model.model_status === 'trained' ? 'Trained Model' : model.model_status }}</span><h2>{{ model.ticker }}</h2><p class="muted">{{ model.model_name }}</p></div>
          <div class="trained-date"><span>Trained</span><strong>{{ model.trained_at | date:'medium' }}</strong></div>
        </div>
        <section class="metric-grid accuracy-metrics">
          <article class="card"><span>Accuracy</span><strong>{{ model.accuracy | percent:'1.0-1' }}</strong><p>Overall correct predictions</p></article>
          <article class="card"><span>Precision</span><strong>{{ model.precision | percent:'1.0-1' }}</strong><p>Quality of predicted UP signals</p></article>
          <article class="card"><span>Recall</span><strong>{{ model.recall | percent:'1.0-1' }}</strong><p>Share of actual UP days found</p></article>
        </section>
        <section class="content-grid">
          <article class="card"><h2>Confusion matrix</h2><div class="matrix"><div><span>True DOWN</span><strong>{{ matrixValue(model, 0, 0) }}</strong></div><div><span>False UP</span><strong>{{ matrixValue(model, 0, 1) }}</strong></div><div><span>False DOWN</span><strong>{{ matrixValue(model, 1, 0) }}</strong></div><div><span>True UP</span><strong>{{ matrixValue(model, 1, 1) }}</strong></div></div></article>
          <article class="card"><h2>Dataset split</h2><div class="row-counts"><div><span>Training rows</span><strong>{{ model.training_rows | number }}</strong></div><div><span>Testing rows</span><strong>{{ model.testing_rows | number }}</strong></div></div><div class="notice">Training: first 80% · Testing: final 20% · No shuffle</div><p class="body-copy">Metrics describe a historical test period and do not guarantee future performance.</p></article>
        </section>
      }
    </main>`,
})
export class ModelAccuracyComponent implements OnInit {
  private readonly api = inject(StockApiService);
  ticker = 'RELIANCE.NS';
  readonly loading = signal(false);
  readonly error = signal('');
  readonly validationError = signal('');
  readonly metrics = signal<TickerModelMetrics | null>(null);

  ngOnInit(): void { this.loadMetrics(); }

  loadMetrics(): void {
    this.validationError.set(tickerValidationMessage(this.ticker));
    if (this.validationError()) return;
    this.ticker = normalizeTicker(this.ticker);
    this.loading.set(true); this.error.set(''); this.metrics.set(null);
    this.api.getModelMetrics(this.ticker).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (metrics) => this.metrics.set(metrics),
      error: (error) => this.error.set(error.status === 404 ? 'No trained model found for this ticker. Analyze or train this stock first.' : error.error?.detail ?? 'Model metrics could not be loaded.'),
    });
  }

  matrixValue(model: TickerModelMetrics, row: number, column: number): number {
    return model.confusion_matrix[row]?.[column] ?? 0;
  }
}
