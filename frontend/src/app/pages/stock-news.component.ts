import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { StockNews } from '../core/models';
import { StockApiService } from '../core/stock-api.service';
import { InfoTipComponent } from '../shared/info-tip.component';

@Component({
  imports: [CommonModule, RouterLink, InfoTipComponent],
  template: `
    <main class="page news-page">
      <div class="news-heading"><div><p class="eyebrow">NEWS & CATALYSTS</p><h1>{{ ticker }}</h1><p class="lead">Source-linked headlines organized by tone, potential impact, and topic. Sentiment is context, not a forecast.</p></div><a class="secondary" [routerLink]="['/stocks', ticker]">Back to research brief</a></div>
      <div class="news-toolbar" aria-label="News lookback period"><span>Lookback</span><div class="period-switcher">@for (option of periods; track option.value) { <button type="button" [class.active]="lookbackDays() === option.value" [disabled]="loading()" (click)="changeLookback(option.value)">{{ option.label }}</button> }</div><button type="button" class="secondary-button" [disabled]="loading()" (click)="load()">Refresh</button></div>
      @if (loading()) { <div class="loading card" role="status"><span class="spinner"></span><strong>Collecting recent headlines...</strong></div> }
      @if (error()) { <div class="error card" role="alert"><p>{{ error() }}</p><button type="button" class="retry-button" (click)="load()">Try again</button></div> }
      @if (news(); as data) {
        <section class="news-summary-grid">
          <article class="card sentiment-card" [class.negative]="data.overall_sentiment === 'NEGATIVE'"><span>HEADLINE SENTIMENT</span><strong>{{ data.overall_sentiment }}</strong><p>Score {{ signedScore(data.sentiment_score) }} from {{ data.article_count }} recent articles</p></article>
          <article class="card"><span>DATA COVERAGE</span><strong>{{ data.coverage }}</strong><p>{{ data.confidence | percent:'1.0-0' }} scoring confidence <app-info-tip text="How reliable the headline scoring appears from available data." /> · {{ data.lookback_days }} day lookback</p></article>
          <article class="card"><span>LEADING CATALYST</span><strong>{{ data.highest_impact_topic ?? 'No recent catalyst' }}</strong><p>{{ data.positive_count }} positive · {{ data.neutral_count }} neutral · {{ data.negative_count }} negative</p></article>
        </section>
        <div class="notice warning news-warning">{{ data.warning }} Updated {{ data.as_of | date:'medium' }} via {{ data.data_source }}.</div>
        <section class="news-list">
          @for (article of data.articles; track article.id) {
            <article class="card news-article">
              <div class="article-meta"><span class="sentiment-pill" [class.positive]="article.sentiment === 'POSITIVE'" [class.negative]="article.sentiment === 'NEGATIVE'">{{ article.sentiment }}</span><span class="impact-pill" [class.high]="article.impact === 'HIGH'">{{ article.impact }} IMPACT</span><span>{{ article.topic }}</span><time>{{ article.published_at | date:'medium' }}</time></div>
              <h2><a [href]="article.url" target="_blank" rel="noopener noreferrer">{{ article.headline }}</a></h2><p class="article-source">{{ article.publisher }}</p><p class="article-summary">{{ article.summary }}</p>
              <div class="why-matters"><strong>Why this may matter</strong><p>{{ article.why_it_matters }}</p></div><a class="source-link" [href]="article.url" target="_blank" rel="noopener noreferrer">Open original source →</a>
            </article>
          } @empty { <div class="empty card"><h2>No recent articles found</h2><p>Technical research is still available. Try a longer lookback or refresh later.</p><a class="button" [routerLink]="['/stocks', ticker]">View technical analysis</a></div> }
        </section>
        <div class="notice news-method">{{ data.methodology }} Educational research only. News tone does not indicate suitability or future returns.</div>
      }
    </main>`,
})
export class StockNewsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute); private readonly api = inject(StockApiService);
  readonly periods = [{ value: 3, label: '3D' }, { value: 7, label: '7D' }, { value: 14, label: '14D' }, { value: 30, label: '30D' }];
  ticker = 'AAPL'; readonly lookbackDays = signal(7); readonly loading = signal(false); readonly error = signal(''); readonly news = signal<StockNews | null>(null);
  ngOnInit(): void { this.route.paramMap.subscribe((params) => { this.ticker = params.get('ticker') ?? 'AAPL'; this.load(); }); }
  changeLookback(days: number): void { if (days !== this.lookbackDays()) { this.lookbackDays.set(days); this.load(); } }
  load(): void { this.loading.set(true); this.error.set(''); this.api.getStockNews(this.ticker, this.lookbackDays()).pipe(finalize(() => this.loading.set(false))).subscribe({ next: (news) => this.news.set(news), error: (error) => { this.news.set(null); this.error.set(error.error?.detail ?? 'Recent stock news could not be loaded.'); } }); }
  signedScore(score: number): string { return `${score > 0 ? '+' : ''}${score.toFixed(2)}`; }
}
