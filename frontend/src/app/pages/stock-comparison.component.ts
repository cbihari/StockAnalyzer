import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { StockAnalysis, StockComparison } from '../core/models';
import { StockApiService } from '../core/stock-api.service';
import { normalizeTicker, tickerValidationMessage } from '../core/ticker-validation';
import { ComparisonChartComponent } from '../shared/comparison-chart.component';
import { TickerAutocompleteComponent } from '../shared/ticker-autocomplete.component';
import { InfoTipComponent } from '../shared/info-tip.component';

@Component({
  imports: [CommonModule, FormsModule, RouterLink, ComparisonChartComponent, TickerAutocompleteComponent, InfoTipComponent],
  template: `
    <main class="page compare-page">
      <p class="eyebrow">MULTI-STOCK RESEARCH</p><h1>Compare the evidence.</h1><p class="lead">Compare two or three stocks across price performance, directional estimates, risk, technical context, and measured model reliability.</p>
      <form class="compare-builder card" (ngSubmit)="compare()">
        <label>Stock 1<app-ticker-autocomplete inputId="compare-one" ariaLabel="First stock" [(value)]="tickerOne" /></label>
        <label>Stock 2<app-ticker-autocomplete inputId="compare-two" ariaLabel="Second stock" [(value)]="tickerTwo" /></label>
        <label>Stock 3 · optional<app-ticker-autocomplete inputId="compare-three" ariaLabel="Third stock optional" [(value)]="tickerThree" /></label>
        <label>Period<select name="period" [(ngModel)]="period"><option value="3mo">3 months</option><option value="6mo">6 months</option><option value="1y">1 year</option><option value="2y">2 years</option><option value="5y">5 years</option></select></label>
        <button type="submit" [disabled]="loading()">{{ loading() ? 'Comparing...' : 'Compare stocks' }}</button>
      </form>
      @if (validationError()) { <div class="error card" role="alert">{{ validationError() }}</div> }
      @if (loading()) { <div class="loading card" role="status"><span class="spinner"></span><strong>Building comparison...</strong><p>A ticker without a model may take longer during its first analysis.</p></div> }
      @if (error()) { <div class="error card" role="alert"><p>{{ error() }}</p><button type="button" class="retry-button" (click)="load()">Try again</button></div> }
      @if (comparison(); as data) {
        <div class="compare-meta"><span>{{ data.period | uppercase }} comparison</span><span>Generated {{ data.generated_at | date:'medium' }}</span></div>
        <section class="card comparison-chart-card"><div class="section-title"><h2>Relative price performance</h2><span>Each stock starts at 100</span></div><app-comparison-chart [stocks]="data.stocks" /><p class="chart-note">Indexed performance compares percentage movement rather than raw prices or currencies.</p></section>
        <section class="comparison-cards">
          @for (stock of data.stocks; track stock.ticker) {
            <article class="card comparison-stock-card">
              <div class="comparison-stock-header"><div><a [routerLink]="['/stocks', stock.ticker]">{{ stock.ticker }}</a><span>{{ stock.trend }} trend</span></div><strong>{{ stock.quote.currency === 'INR' ? '₹' : '$' }}{{ stock.quote.latestPrice | number:'1.2-2' }}</strong></div>
              <div class="comparison-metrics"><div><span>Estimate</span><strong [class.negative]="stock.prediction.prediction === 'DOWN'">{{ stock.prediction.prediction }}</strong><small>{{ stock.prediction.confidence }}% confidence <app-info-tip text="How strongly the model favors its estimate based on current inputs." /></small></div><div><span>Risk level <app-info-tip text="Overall uncertainty based on volatility, signal agreement, and data quality." /></span><strong>{{ stock.risk.level }}</strong><small>{{ stock.risk.score }}/100</small></div><div><span>Model accuracy <app-info-tip text="Share of historical test predictions the model got right." /></span><strong>{{ stock.prediction.model_accuracy === null ? 'N/A' : (stock.prediction.model_accuracy | percent:'1.0-1') }}</strong><small>Historical holdout</small></div><div><span>Period return</span><strong [class.negative]="periodReturn(stock) < 0">{{ periodReturn(stock) | percent:'1.1-1' }}</strong><small>{{ data.period | uppercase }}</small></div></div>
              <div class="comparison-context"><p><b>RSI <app-info-tip text="Shows whether recent price moves may be overbought or oversold." /></b> {{ stock.indicators.latest.RSI_14 | number:'1.1-1' }}</p><p><b>Volatility <app-info-tip text="How widely and quickly the stock price tends to move." /></b> {{ stock.marketContext.annualizedVolatility | percent:'1.1-1' }}</p><p><b>Support <app-info-tip text="A recent price area where declines have often slowed." /></b> {{ stock.quote.currency === 'INR' ? '₹' : '$' }}{{ stock.marketContext.support | number:'1.2-2' }}</p><p><b>Resistance <app-info-tip text="A recent price area where advances have often slowed." /></b> {{ stock.quote.currency === 'INR' ? '₹' : '$' }}{{ stock.marketContext.resistance | number:'1.2-2' }}</p></div>
              <div class="comparison-evidence"><span>{{ stock.supportingSignals.length }} supporting</span><span>{{ stock.conflictingSignals.length }} conflicting</span><span>{{ modelLabel(stock) }}</span></div>
            </article>
          }
        </section>
        <div class="notice warning">{{ data.disclaimer }}</div>
      }
    </main>`,
})
export class StockComparisonComponent implements OnInit {
  private readonly api = inject(StockApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly comparison = signal<StockComparison | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly validationError = signal('');
  tickerOne = 'AAPL'; tickerTwo = 'MSFT'; tickerThree = ''; period = '1y';

  ngOnInit(): void {
    const tickers = (this.route.snapshot.queryParamMap.get('tickers') ?? 'AAPL,MSFT').split(',');
    this.tickerOne = tickers[0] ?? 'AAPL'; this.tickerTwo = tickers[1] ?? 'MSFT'; this.tickerThree = tickers[2] ?? '';
    this.period = this.route.snapshot.queryParamMap.get('period') ?? '1y'; this.load();
  }

  compare(): void {
    const tickers = this.validatedTickers();
    if (!tickers) return;
    this.router.navigate([], { relativeTo: this.route, queryParams: { tickers: tickers.join(','), period: this.period }, replaceUrl: true });
    this.load(tickers);
  }

  load(prevalidated?: string[]): void {
    const tickers = prevalidated ?? this.validatedTickers();
    if (!tickers) return;
    this.loading.set(true); this.error.set(''); this.comparison.set(null);
    this.api.compareStocks(tickers, this.period).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (comparison) => this.comparison.set(comparison),
      error: (error) => this.error.set(error.error?.detail ?? 'The comparison could not be completed.'),
    });
  }

  periodReturn(stock: StockAnalysis): number { const first = stock.history[0]?.close; const last = stock.history.at(-1)?.close; return first && last ? (last - first) / first : 0; }
  modelLabel(stock: StockAnalysis): string { return stock.prediction.model_status === 'rule_based_fallback' ? 'Rule fallback' : stock.prediction.model_status === 'newly_trained_model' ? 'New ML model' : 'Existing ML model'; }

  private validatedTickers(): string[] | null {
    this.validationError.set('');
    const candidates = [this.tickerOne, this.tickerTwo, this.tickerThree].map(normalizeTicker).filter(Boolean);
    const invalid = candidates.find((ticker) => tickerValidationMessage(ticker));
    if (invalid) { this.validationError.set(tickerValidationMessage(invalid)); return null; }
    const unique = [...new Set(candidates)];
    if (unique.length < 2 || unique.length > 3) { this.validationError.set('Choose 2 or 3 unique ticker symbols.'); return null; }
    return unique;
  }
}
