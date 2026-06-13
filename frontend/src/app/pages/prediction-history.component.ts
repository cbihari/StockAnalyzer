import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { PersistedPredictionHistoryItem, PredictionHistoryResponse } from '../core/models';
import { StockApiService } from '../core/stock-api.service';
import { InfoTipComponent } from '../shared/info-tip.component';

@Component({
  imports: [CommonModule, FormsModule, RouterLink, InfoTipComponent],
  template: `
    <main class="page history-page">
      <div class="history-heading">
        <div><p class="eyebrow">PREDICTION ACCOUNTABILITY</p><h1>Research outcomes</h1><p class="lead">Review what the system estimated, what happened on the next eligible trading day, and where uncertainty mattered.</p></div>
        <button type="button" class="secondary-button" [disabled]="evaluating()" (click)="evaluate()">{{ evaluating() ? 'Checking outcomes...' : 'Evaluate pending' }}</button>
      </div>

      @if (evaluationMessage()) { <div class="notice" role="status">{{ evaluationMessage() }}</div> }
      @if (error()) { <div class="error card" role="alert"><p>{{ error() }}</p><button type="button" class="retry-button" (click)="load()">Try again</button></div> }

      @if (history(); as data) {
        <section class="history-summary-grid">
          <article class="card"><span>FILTERED RECORDS</span><strong>{{ data.total }}</strong><p>Latest persisted prediction snapshots</p></article>
          <article class="card"><span>EVALUATED ACCURACY</span><strong>{{ data.evaluated ? (data.accuracy_percentage / 100 | percent:'1.0-1') : 'N/A' }}</strong><p>{{ data.correct }} correct · {{ data.wrong }} wrong</p></article>
          <article class="card"><span>PENDING</span><strong>{{ data.pending }}</strong><p>Waiting for the next eligible trading-day close</p></article>
        </section>

        <form class="history-filters card" (ngSubmit)="load()">
          <label for="history-ticker">Ticker<input id="history-ticker" name="ticker" [(ngModel)]="ticker" placeholder="e.g. RELIANCE.NS or AAPL" /></label>
          <label for="history-outcome">Outcome <app-info-tip text="What actually happened after the prediction" /><select id="history-outcome" name="outcome" [(ngModel)]="outcome"><option value="all">All outcomes</option><option value="pending">Pending</option><option value="correct">Correct</option><option value="wrong">Wrong</option></select></label>
          <button type="submit" [disabled]="loading()">{{ loading() ? 'Filtering...' : 'Apply filters' }}</button>
          <button type="button" class="secondary-button" [disabled]="!data.items.length" (click)="exportCsv(data.items)">Export CSV</button>
        </form>

        @if (loading()) { <div class="loading card" role="status"><span class="spinner"></span><strong>Loading prediction history...</strong></div> }
        @else if (data.items.length) {
          <div class="table-card card history-table"><table><thead><tr><th>Ticker</th><th>Estimate</th><th>Confidence <app-info-tip text="How strongly the model favors its estimate based on current inputs." /></th><th>Actual</th><th>Outcome</th><th>Model</th><th>Created</th><th>Review</th></tr></thead><tbody>
            @for (item of data.items; track item.id) {
              <tr><td><a [routerLink]="['/stocks', item.ticker]">{{ item.ticker }}</a></td><td><span class="badge" [class.down]="item.prediction === 'DOWN'">{{ item.prediction }}</span></td><td>{{ item.confidence }}%</td><td>{{ item.actual_result ?? 'Pending' }}</td><td><span class="outcome-badge" [class.correct]="item.is_correct === true" [class.wrong]="item.is_correct === false">{{ outcomeLabel(item) }}</span></td><td>{{ modelLabel(item) }}</td><td>{{ item.created_at | date:'medium' }}</td><td><details><summary>Explain</summary><p>{{ outcomeExplanation(item) }}</p></details></td></tr>
            }
          </tbody></table></div>
        } @else { <div class="empty card"><h2>No matching predictions</h2><p>Adjust the filters or analyze a stock to create a new research snapshot.</p><a class="button" routerLink="/search">Research a stock</a></div> }

        <div class="notice warning history-disclaimer">Accuracy here measures directional matches across evaluated snapshots. It does not measure returns, profitability, or future reliability.</div>
      }
    </main>`,
})
export class PredictionHistoryComponent implements OnInit {
  private readonly api = inject(StockApiService);
  readonly history = signal<PredictionHistoryResponse | null>(null);
  readonly loading = signal(false);
  readonly evaluating = signal(false);
  readonly error = signal('');
  readonly evaluationMessage = signal('');
  ticker = '';
  outcome = 'all';

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true); this.error.set('');
    this.api.getPredictionHistory(this.ticker.toUpperCase(), this.outcome).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (history) => this.history.set(history),
      error: (error) => this.error.set(error.error?.detail ?? 'Prediction history could not be loaded.'),
    });
  }

  evaluate(): void {
    this.evaluating.set(true); this.evaluationMessage.set(''); this.error.set('');
    this.api.evaluatePredictions().pipe(finalize(() => this.evaluating.set(false))).subscribe({
      next: (result) => { this.evaluationMessage.set(`${result.evaluatedPredictions} predictions evaluated. ${result.pendingPredictions} remain pending.`); this.load(); },
      error: (error) => this.error.set(error.error?.detail ?? 'Pending predictions could not be evaluated.'),
    });
  }

  outcomeLabel(item: PersistedPredictionHistoryItem): string { return item.is_correct === null ? 'Pending' : item.is_correct ? 'Correct' : 'Wrong'; }
  modelLabel(item: PersistedPredictionHistoryItem): string { return item.prediction_type === 'rule_based_fallback' ? 'Rule fallback' : item.model_status === 'newly_trained_model' ? 'New ML model' : 'ML model'; }
  outcomeExplanation(item: PersistedPredictionHistoryItem): string {
    if (item.is_correct === null) return 'This estimate is waiting for the next eligible trading-day close. Weekends and exchange holidays can extend the wait.';
    if (item.is_correct) return `The next eligible close moved ${item.actual_result}, matching the ${item.prediction} estimate. One correct result does not establish future reliability.`;
    return `The next eligible close moved ${item.actual_result}, against the ${item.prediction} estimate. Technical signals describe probabilities, while news, gaps, volatility, and changing market conditions can produce a different outcome.`;
  }

  exportCsv(items: PersistedPredictionHistoryItem[]): void {
    const header = ['ticker','prediction','confidence','probability_up','probability_down','actual_result','is_correct','prediction_type','model_status','model_accuracy','created_at'];
    const rows = items.map((item) => [item.ticker,item.prediction,item.confidence,item.probability_up ?? '',item.probability_down ?? '',item.actual_result ?? '',item.is_correct ?? '',item.prediction_type,item.model_status ?? '',item.model_accuracy ?? '',item.created_at]);
    const csv = [header, ...rows].map((row) => row.map((value) => `"${String(value).replaceAll('"', '""')}"`).join(',')).join('\n');
    const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
    const link = document.createElement('a'); link.href = url; link.download = `stockanalyzer-predictions-${new Date().toISOString().slice(0, 10)}.csv`; link.click(); URL.revokeObjectURL(url);
  }
}
