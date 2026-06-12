import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, map, switchMap } from 'rxjs/operators';
import { AiExplanationResponse, MlPrediction, StockAnalysis, StockNews } from '../core/models';
import { PredictionHistoryService } from '../core/prediction-history.service';
import { StockApiService } from '../core/stock-api.service';
import { PriceChartComponent } from '../shared/price-chart.component';
import { TickerAutocompleteComponent } from '../shared/ticker-autocomplete.component';
import { normalizeTicker, tickerValidationMessage } from '../core/ticker-validation';
import { WatchlistService } from '../core/watchlist.service';

@Component({
  imports: [CommonModule, FormsModule, RouterLink, PriceChartComponent, TickerAutocompleteComponent],
  template: `
    <main class="page">
      <form class="top-search" (ngSubmit)="search()" novalidate><app-ticker-autocomplete inputId="detail-ticker" ariaLabel="Ticker" [(value)]="ticker" (valueChange)="validationError.set('')" (submitted)="search()" /><button type="submit" [disabled]="loading() || retraining()">{{ loading() ? 'Analyzing...' : 'Analyze' }}</button></form>
      <div class="research-toolbar" aria-label="Analysis period">
        <span>Research period</span>
        <div class="period-switcher">
          @for (option of periods; track option.value) {
            <button type="button" [class.active]="period() === option.value" [attr.aria-pressed]="period() === option.value" [disabled]="loading()" (click)="changePeriod(option.value)">{{ option.label }}</button>
          }
        </div>
      </div>
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
        @if (analysis(); as research) {
          <div class="research-header">
            <div><p class="eyebrow">EXPLAINABLE RESEARCH BRIEF</p><div class="ticker-title"><h1>{{ result.ticker }}</h1><span>{{ research.trend }} TREND</span><button type="button" class="watchlist-toggle" [class.saved]="watchlist.has(result.ticker)" (click)="toggleWatchlist(result.ticker)">{{ watchlist.has(result.ticker) ? 'Saved to watchlist' : 'Add to watchlist' }}</button></div><p class="muted">Generated {{ research.generated_at | date:'medium' }} · Source {{ research.data_source }}</p></div>
            <div class="quote-block"><span>Latest close · {{ research.quote.as_of }}</span><strong>{{ research.quote.currency === 'INR' ? '₹' : '$' }}{{ research.quote.latestPrice | number:'1.2-2' }}</strong><small [class.negative]="research.quote.daily_change < 0">{{ research.quote.daily_change >= 0 ? '+' : '' }}{{ research.quote.daily_change | number:'1.2-2' }} · {{ research.quote.daily_change_percent | percent:'1.2-2' }}</small></div>
          </div>
        }
        @if (result.model_trained) { <div class="notice" role="status">A new Random Forest model was trained and saved for {{ result.ticker }}.</div> }
        @if (result.fallback_used) { <div class="notice warning" role="status">There was not enough history to train a reliable ML model, so this result uses the rule-based fallback.</div> }
        <div class="notice model-summary">
          <span class="badge model-badge" [class.new-model]="result.model_status === 'newly_trained_model'" [class.fallback]="result.model_status === 'rule_based_fallback'">{{ modelStatusLabel(result.model_status) }}</span>
          @if (result.model_accuracy !== null) { <span>Accuracy {{ result.model_accuracy | percent:'1.0-1' }}</span> }
          <span>{{ result.warning }}</span>
          <button type="button" class="secondary-button compare-button" (click)="openComparison()">Compare</button>
          <button type="button" class="secondary-button retrain-button" [disabled]="retraining()" (click)="retrainModel()">{{ retraining() ? 'Retraining...' : 'Retrain Model' }}</button>
        </div>
        @if (retraining()) { <div class="training-progress" role="status" aria-live="polite"><span class="spinner" aria-hidden="true"></span> Training model with the latest 5 years of data. This may take a moment...</div> }
        @if (retrainSuccess()) { <div class="success-message" role="status">{{ retrainSuccess() }}</div> }
        @if (retrainError()) { <div class="error-message" role="alert">{{ retrainError() }}</div> }
        @if (analysis(); as research) {
          <section class="research-summary-grid">
            <article class="prediction-card card" [class.down]="result.prediction === 'DOWN'"><span>NEXT TRADING-DAY ESTIMATE</span><div class="signal-line"><strong>{{ result.prediction }}</strong><b>{{ result.confidence }}%</b></div><p>Model confidence, not probability of profit</p><div class="probability-track"><i [style.width.%]="result.probability_up * 100"></i></div><div class="probability-labels"><span>UP {{ result.probability_up | percent:'1.0-1' }}</span><span>DOWN {{ result.probability_down | percent:'1.0-1' }}</span></div></article>
            <article class="risk-card card" [class.high]="research.risk.level === 'HIGH'"><span>RISK ASSESSMENT</span><div class="risk-score"><strong>{{ research.risk.level }}</strong><b>{{ research.risk.score }}/100</b></div><div class="risk-track"><i [style.width.%]="research.risk.score"></i></div><p>{{ research.risk.summary }}</p></article>
            <article class="card model-reliability"><span>MODEL RELIABILITY</span><strong>{{ result.model_accuracy === null ? 'N/A' : (result.model_accuracy | percent:'1.0-1') }}</strong><p>Accuracy across historical holdout cases, not this specific prediction.</p><small>{{ modelStatusLabel(result.model_status) }}</small></article>
          </section>
          <section class="market-context-grid">
            <article class="card level-card"><div class="section-title"><h2>Price structure</h2><span>{{ research.marketContext.lookbackSessions }} sessions</span></div><div class="level-values"><div><span>Support</span><strong>{{ research.quote.currency === 'INR' ? '₹' : '$' }}{{ research.marketContext.support | number:'1.2-2' }}</strong></div><div><span>Resistance</span><strong>{{ research.quote.currency === 'INR' ? '₹' : '$' }}{{ research.marketContext.resistance | number:'1.2-2' }}</strong></div></div><div class="range-track" aria-label="Current price position inside recent range"><i [style.left.%]="research.marketContext.rangePosition * 100"></i></div><div class="range-labels"><span>Recent low</span><b>{{ research.marketContext.rangePosition | percent:'1.0-0' }} through range</b><span>Recent high</span></div></article>
            <article class="card volatility-card"><div class="section-title"><h2>Volatility context</h2><span>Annualized estimate</span></div><strong>{{ research.marketContext.annualizedVolatility | percent:'1.1-1' }}</strong><p>{{ volatilityLabel(research.marketContext.annualizedVolatility) }}</p><small>Average daily high-low range {{ research.marketContext.averageDailyRange | percent:'1.1-1' }}</small></article>
            <article class="card invalidation-card"><div class="section-title"><h2>What could weaken this view?</h2><span>Invalidation</span></div><p>{{ research.marketContext.invalidation }}</p><small>This is research context, not a stop-loss or trading instruction.</small></article>
          </section>
          <section class="evidence-grid">
            <article class="card evidence-card"><div class="section-title"><h2>Supporting evidence</h2><span>{{ research.supportingSignals.length }} signals</span></div>@if (research.supportingSignals.length) { <ul>@for (signal of research.supportingSignals; track signal.label) { <li><span>+</span><div><strong>{{ signal.label }}</strong><p>{{ signal.detail }}</p></div></li> }</ul> } @else { <p class="muted">No strong supporting signal is available.</p> }</article>
            <article class="card evidence-card conflicts"><div class="section-title"><h2>Conflicting evidence</h2><span>{{ research.conflictingSignals.length }} signals</span></div>@if (research.conflictingSignals.length) { <ul>@for (signal of research.conflictingSignals; track signal.label) { <li><span>!</span><div><strong>{{ signal.label }}</strong><p>{{ signal.detail }}</p></div></li> }</ul> } @else { <p class="muted">The available technical signals are aligned.</p> }</article>
          </section>
          <section class="card catalyst-preview">
            <div class="section-title"><h2>News sentiment & catalysts</h2><a [routerLink]="['/stocks', result.ticker, 'news']">View all news →</a></div>
            @if (newsLoading()) { <p class="muted"><span class="spinner"></span>Collecting recent headlines...</p> }
            @if (newsError()) { <p class="muted">{{ newsError() }} Technical analysis remains available.</p> }
            @if (news(); as newsData) {
              <div class="catalyst-summary"><div><span>Headline tone</span><strong [class.negative]="newsData.overall_sentiment === 'NEGATIVE'">{{ newsData.overall_sentiment }}</strong></div><div><span>Coverage</span><strong>{{ newsData.coverage }}</strong></div><div><span>Leading catalyst</span><strong>{{ newsData.highest_impact_topic ?? 'None detected' }}</strong></div><div><span>Article mix</span><strong>{{ newsData.positive_count }}+ / {{ newsData.neutral_count }}= / {{ newsData.negative_count }}−</strong></div></div>
              <div class="catalyst-headlines">@for (article of newsData.articles.slice(0, 3); track article.id) { <a [href]="article.url" target="_blank" rel="noopener noreferrer"><span>{{ article.impact }} · {{ article.topic }}</span><strong>{{ article.headline }}</strong><small>{{ article.publisher }} · {{ article.published_at | date:'short' }}</small></a> } @empty { <p class="muted">No recent provider headlines were found.</p> }</div>
              <p class="catalyst-warning">{{ newsData.warning }}</p>
            }
          </section>
          <section class="card ai-research-panel" aria-labelledby="ai-research-title">
            <div class="section-title ai-research-heading">
              <div><p class="eyebrow">OPTIONAL GENERATED RESEARCH</p><h2 id="ai-research-title">AI Research Explanation</h2></div>
              @if (aiExplanation(); as ai) { <span class="badge" [class.fallback]="ai.fallbackUsed">{{ ai.fallbackUsed ? 'Deterministic Fallback' : 'OpenAI Generated' }}</span> }
            </div>
            @if (!aiExplanation() && !aiLoading()) {
              <div class="ai-generate-state"><p>AI explanations may contain errors. Review the underlying data and indicators.</p><button type="button" (click)="generateAiExplanation(false)">Generate AI Explanation</button></div>
            }
            @if (aiLoading()) { <div class="ai-skeleton" role="status" aria-live="polite"><span></span><span></span><span></span><p>Generating a grounded explanation from the current analysis...</p></div> }
            @if (aiError()) { <div class="error-message" role="alert">{{ aiError() }} <button type="button" class="secondary-button" (click)="generateAiExplanation(false)">Try again</button></div> }
            @if (aiExplanation(); as ai) {
              <div class="ai-overview"><div><span>Prediction</span><strong>{{ ai.explanation.prediction }} · {{ ai.explanation.confidence }}%</strong></div><div><span>Risk level</span><strong [class.negative]="ai.explanation.risk_level === 'HIGH'">{{ ai.explanation.risk_level }}</strong></div><div><span>Generated</span><strong>{{ ai.generatedAt | date:'short' }}</strong></div></div>
              <p class="ai-summary">{{ ai.explanation.summary }}</p>
              <div class="ai-columns"><div><h3>Supporting signals</h3><ul>@for (item of ai.explanation.supporting_signals; track item.signal) { <li><strong>{{ item.signal }}</strong><span>{{ item.explanation }}</span></li> } @empty { <li>No strong supporting signals were identified.</li> }</ul></div><div><h3>Conflicting signals</h3><ul>@for (item of ai.explanation.conflicting_signals; track item.signal) { <li><strong>{{ item.signal }}</strong><span>{{ item.explanation }}</span></li> } @empty { <li>No material conflicting signals were identified.</li> }</ul></div></div>
              <div class="ai-columns"><div><h3>Risk factors</h3><ul>@for (item of ai.explanation.risk_factors; track item) { <li>{{ item }}</li> } @empty { <li>No additional risk factors were listed.</li> }</ul></div><div><h3>What could change the view</h3><ul>@for (item of ai.explanation.what_could_change_the_view; track item) { <li>{{ item }}</li> }</ul></div></div>
              <div class="ai-beginner"><h3>In plain language</h3><p>{{ ai.explanation.beginner_explanation }}</p></div>
              @if (ai.explanation.data_limitations.length) { <div class="ai-limitations"><h3>Data limitations</h3><ul>@for (item of ai.explanation.data_limitations; track item) { <li>{{ item }}</li> }</ul></div> }
              <div class="ai-footer"><small>{{ ai.explanation.disclaimer }}</small><button type="button" class="secondary-button" [disabled]="aiLoading()" (click)="generateAiExplanation(true)">Regenerate</button></div>
            }
          </section>
        }
        <section class="content-grid">
          <article class="card chart-card"><div class="section-title"><h2>Closing price</h2><span>{{ periodLabel() }}</span></div><app-price-chart [prices]="history()" /></article>
          <article class="card"><h2>Why this prediction?</h2><ul class="reasons">@for (reason of result.reasons; track reason) { <li>{{ reason }}</li> }</ul></article>
        </section>
        @if (indicators(); as data) {
          <section class="card indicators"><div class="section-title"><h2>Technical indicators</h2><span>{{ data.latest.date }}</span></div>
            <div class="indicator-grid">
              <div><span>RSI 14 <a routerLink="/learn/rsi">Learn</a></span><strong>{{ data.latest.RSI_14 | number:'1.2-2' }}</strong></div><div><span>SMA 20 <a routerLink="/learn/sma">Learn</a></span><strong>{{ data.latest.SMA_20 | number:'1.2-2' }}</strong></div><div><span>SMA 50 <a routerLink="/learn/sma">Learn</a></span><strong>{{ data.latest.SMA_50 | number:'1.2-2' }}</strong></div><div><span>EMA 20 <a routerLink="/learn/ema">Learn</a></span><strong>{{ data.latest.EMA_20 | number:'1.2-2' }}</strong></div><div><span>EMA 50 <a routerLink="/learn/ema">Learn</a></span><strong>{{ data.latest.EMA_50 | number:'1.2-2' }}</strong></div><div><span>MACD <a routerLink="/learn/macd">Learn</a></span><strong>{{ data.latest.MACD | number:'1.2-2' }}</strong></div><div><span>MACD signal <a routerLink="/learn/macd">Learn</a></span><strong>{{ data.latest.MACD_signal | number:'1.2-2' }}</strong></div><div><span>Volume change <a routerLink="/learn/volume">Learn</a></span><strong>{{ data.latest.volume_change | percent:'1.0-1' }}</strong></div>
            </div>
          </section>
        }
      }
    </main>`,
})
export class StockDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute); private readonly router = inject(Router); private readonly api = inject(StockApiService); private readonly historyStore = inject(PredictionHistoryService);
  readonly watchlist = inject(WatchlistService);
  private trainingMessageTimer: ReturnType<typeof setTimeout> | undefined;
  readonly periods = [{ value: '3mo', label: '3M' }, { value: '6mo', label: '6M' }, { value: '1y', label: '1Y' }, { value: '2y', label: '2Y' }, { value: '5y', label: '5Y' }] as const;
  ticker = 'RELIANCE.NS'; readonly period = signal('1y'); readonly loading = signal(false); readonly retraining = signal(false); readonly retrainSuccess = signal(''); readonly retrainError = signal(''); readonly loadingMessage = signal('Checking model for this stock...'); readonly error = signal(''); readonly validationError = signal(''); readonly analysis = signal<StockAnalysis | null>(null); readonly history = signal<StockAnalysis['history']>([]); readonly indicators = signal<StockAnalysis['indicators'] | null>(null); readonly prediction = signal<MlPrediction | null>(null); readonly news = signal<StockNews | null>(null); readonly newsLoading = signal(false); readonly newsError = signal(''); readonly aiExplanation = signal<AiExplanationResponse | null>(null); readonly aiLoading = signal(false); readonly aiError = signal('');
  ngOnInit(): void { this.route.paramMap.subscribe((params) => { this.ticker = params.get('ticker') ?? 'RELIANCE.NS'; this.aiExplanation.set(null); this.aiError.set(''); this.load(); this.loadNews(); }); }
  ngOnDestroy(): void { this.clearTrainingMessageTimer(); }
  search(): void { this.validationError.set(tickerValidationMessage(this.ticker)); if (!this.validationError()) this.router.navigate(['/stocks', normalizeTicker(this.ticker)]); }
  load(): void {
    this.clearTrainingMessageTimer(); this.loading.set(true); this.loadingMessage.set('Checking model for this stock...'); this.error.set(''); this.analysis.set(null); this.prediction.set(null);
    this.trainingMessageTimer = setTimeout(() => this.loadingMessage.set('Training model for this stock for the first time...'), 1800);
    this.api.getStockAnalysis(this.ticker, this.period()).subscribe({
      next: (analysis) => {
        this.clearTrainingMessageTimer();
        if (analysis.prediction.model_status === 'newly_trained_model') {
          this.loadingMessage.set('Training model for this stock for the first time...');
          this.trainingMessageTimer = setTimeout(() => this.showResult(analysis), 700);
          return;
        }
        this.showResult(analysis);
      },
      error: (error) => { this.clearTrainingMessageTimer(); this.loading.set(false); this.error.set(error.error?.detail ?? 'We could not load this stock. Check the ticker and try again.'); },
    });
  }
  changePeriod(period: string): void { if (period !== this.period()) { this.period.set(period); this.load(); } }
  loadNews(): void { this.newsLoading.set(true); this.newsError.set(''); this.api.getStockNews(this.ticker, 7, 6).pipe(finalize(() => this.newsLoading.set(false))).subscribe({ next: (news) => this.news.set(news), error: () => { this.news.set(null); this.newsError.set('Recent news could not be loaded.'); } }); }
  periodLabel(): string { return this.periods.find((option) => option.value === this.period())?.label ?? this.period().toUpperCase(); }
  volatilityLabel(value: number): string { return value >= .45 ? 'High recent volatility: price outcomes may vary widely.' : value >= .3 ? 'Moderate-to-high recent volatility.' : value >= .18 ? 'Moderate recent volatility.' : 'Relatively low recent volatility.'; }
  modelStatusLabel(status: MlPrediction['model_status']): string { return status === 'existing_model' ? 'Existing ML Model' : status === 'newly_trained_model' ? 'Newly Trained ML Model' : 'Rule-Based Fallback'; }
  openComparison(): void {
    const ticker = normalizeTicker(this.ticker);
    const counterpart = ticker === 'MSFT' ? 'AAPL' : 'MSFT';
    this.router.navigate(['/compare'], { queryParams: { tickers: `${ticker},${counterpart}`, period: this.period() } });
  }
  toggleWatchlist(ticker: string): void { this.watchlist.toggle(ticker); }
  generateAiExplanation(forceRefresh: boolean): void {
    if (this.aiLoading()) return;
    this.aiLoading.set(true); this.aiError.set('');
    this.api.generateAiExplanation(this.ticker, forceRefresh).pipe(finalize(() => this.aiLoading.set(false))).subscribe({
      next: (explanation) => this.aiExplanation.set(explanation),
      error: (error) => this.aiError.set(error.status === 504 ? 'The explanation request timed out. Please try again.' : error.error?.detail ?? 'AI explanation is temporarily unavailable.'),
    });
  }
  retrainModel(): void {
    if (this.retraining()) return;
    this.retraining.set(true); this.retrainSuccess.set(''); this.retrainError.set('');
    this.api.retrainModel(this.ticker).pipe(
      switchMap((training) => this.api.getStockAnalysis(this.ticker, this.period()).pipe(
        map((analysis) => ({ training, analysis })),
      )),
      finalize(() => this.retraining.set(false)),
    ).subscribe({
      next: ({ training, analysis }) => {
        this.showResult(analysis);
        this.retrainSuccess.set(`Model retrained successfully. Accuracy ${(training.accuracy * 100).toFixed(1)}%, precision ${(training.precision * 100).toFixed(1)}%, recall ${(training.recall * 100).toFixed(1)}%. Prediction refreshed.`);
      },
      error: (error) => this.retrainError.set(error.error?.detail ?? 'The model could not be retrained. Please try again.'),
    });
  }
  private showResult(analysis: StockAnalysis): void { this.clearTrainingMessageTimer(); this.analysis.set(analysis); this.history.set(analysis.history); this.indicators.set(analysis.indicators); this.prediction.set(analysis.prediction); this.historyStore.add(analysis.prediction); this.loading.set(false); }
  private clearTrainingMessageTimer(): void { if (this.trainingMessageTimer !== undefined) { clearTimeout(this.trainingMessageTimer); this.trainingMessageTimer = undefined; } }
}
