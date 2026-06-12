import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { MarketInstrument, MarketOverview } from '../core/models';
import { StockApiService } from '../core/stock-api.service';
import { SparklineComponent } from '../shared/sparkline.component';

@Component({
  imports: [CommonModule, RouterLink, SparklineComponent],
  template: `
    <main class="page market-page">
      <div class="market-heading"><div><p class="eyebrow">MARKET CONTEXT</p><h1>See the wider market.</h1><p class="lead">Track major indices and a transparent liquid-stock sample before interpreting any single-stock signal.</p></div><div class="region-switcher" aria-label="Market region"><button type="button" [class.active]="region() === 'india'" (click)="selectRegion('india')">India</button><button type="button" [class.active]="region() === 'us'" (click)="selectRegion('us')">United States</button></div></div>
      @if (loading()) { <div class="loading card" role="status"><span class="spinner"></span><strong>Loading market context...</strong></div> }
      @if (error()) { <div class="error card" role="alert"><p>{{ error() }}</p><button type="button" class="retry-button" (click)="load()">Try again</button></div> }
      @if (overview(); as market) {
        <div class="market-meta"><span>{{ market.session_status.replaceAll('_', ' ') }}</span><span>Updated {{ market.as_of | date:'medium' }} · {{ market.data_source }}</span><button type="button" class="secondary-button" [disabled]="loading()" (click)="load()">Refresh</button></div>
        <section class="index-grid">
          @for (index of market.indices; track index.symbol) {
            <article class="card index-card"><div><span>{{ index.symbol }}</span><h2>{{ index.name }}</h2></div><strong>{{ index.price | number:'1.2-2' }}</strong><small [class.negative]="index.change < 0">{{ index.change >= 0 ? '+' : '' }}{{ index.change | number:'1.2-2' }} · {{ index.change_percent | percent:'1.2-2' }}</small><app-sparkline [values]="index.sparkline" [negative]="index.change < 0" /><div class="day-range"><span>Low {{ index.day_low | number:'1.2-2' }}</span><span>High {{ index.day_high | number:'1.2-2' }}</span></div></article>
          }
        </section>
        <section class="market-context-row">
          <article class="card breadth-card"><div class="section-title"><h2>Market breadth</h2><span>{{ market.breadth.coverage }} tracked stocks</span></div><strong [class.negative]="market.breadth.sentiment === 'NEGATIVE'">{{ market.breadth.sentiment }}</strong><div class="breadth-track"><i [style.width.%]="breadthPercent(market)"></i></div><div class="breadth-counts"><span><b>{{ market.breadth.advancers }}</b> Advancing</span><span><b>{{ market.breadth.unchanged }}</b> Unchanged</span><span><b>{{ market.breadth.decliners }}</b> Declining</span></div></article>
          <article class="card insight-card"><div class="section-title"><h2>Market read</h2><span>Rule based</span></div><ul>@for (insight of market.insights; track insight) { <li>{{ insight }}</li> }</ul></article>
        </section>
        <section class="mover-grid">
          <article class="card mover-card"><div class="section-title"><h2>Top gainers</h2><span>Tracked universe</span></div><ng-container *ngTemplateOutlet="moverTable; context: { items: market.top_gainers }" /></article>
          <article class="card mover-card"><div class="section-title"><h2>Top losers</h2><span>Tracked universe</span></div><ng-container *ngTemplateOutlet="moverTable; context: { items: market.top_losers }" /></article>
          <article class="card mover-card"><div class="section-title"><h2>Most active</h2><span>By volume</span></div><ng-container *ngTemplateOutlet="moverTable; context: { items: market.most_active }" /></article>
        </section>
        <ng-template #moverTable let-items="items"><div class="mover-list">@for (item of items; track item.symbol) { <a [routerLink]="['/stocks', item.symbol]"><div><strong>{{ item.symbol }}</strong><small>{{ item.name }}</small></div><div><b>{{ currency(market) }}{{ item.price | number:'1.2-2' }}</b><span [class.negative]="item.change < 0">{{ item.change_percent | percent:'1.2-2' }}</span></div></a> } @empty { <p class="mover-empty">No matching stocks in the tracked sample.</p> }</div></ng-template>
        <div class="notice warning market-coverage">{{ market.coverage_note }} Delayed/end-of-day market context only.</div>
      }
    </main>`,
})
export class MarketOverviewComponent implements OnInit {
  private readonly api = inject(StockApiService);
  readonly region = signal<'india' | 'us'>('india'); readonly overview = signal<MarketOverview | null>(null); readonly loading = signal(false); readonly error = signal('');
  ngOnInit(): void { this.load(); }
  selectRegion(region: 'india' | 'us'): void { if (region !== this.region()) { this.region.set(region); this.load(); } }
  load(): void { this.loading.set(true); this.error.set(''); this.api.getMarketOverview(this.region()).pipe(finalize(() => this.loading.set(false))).subscribe({ next: (market) => this.overview.set(market), error: (error) => this.error.set(error.error?.detail ?? 'Market context could not be loaded.') }); }
  breadthPercent(market: MarketOverview): number { return market.breadth.coverage ? (market.breadth.advancers / market.breadth.coverage) * 100 : 0; }
  currency(market: MarketOverview): string { return market.region === 'india' ? '₹' : '$'; }
}
