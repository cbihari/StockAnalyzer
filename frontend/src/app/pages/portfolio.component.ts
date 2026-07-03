import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { PortfolioSummary } from '../core/models';
import { PortfolioService } from '../core/portfolio.service';
import { StockApiService } from '../core/stock-api.service';
import { normalizeTicker, tickerValidationMessage } from '../core/ticker-validation';
import { TickerAutocompleteComponent } from '../shared/ticker-autocomplete.component';

@Component({
  imports: [CommonModule, FormsModule, RouterLink, TickerAutocompleteComponent],
  template: `
    <main class="page portfolio-page">
      <div class="portfolio-heading"><div><p class="eyebrow">PORTFOLIO RESEARCH</p><h1>Understand what you own.</h1><p class="lead">Track manually entered holdings, performance, currency buckets, and concentration without connecting a brokerage account.</p></div><button type="button" class="secondary-button" [disabled]="loading()" (click)="loadSummary()">Refresh values</button></div>
      <div class="notice"><strong>{{ syncLabel() }}</strong> Holdings are stored in your anonymous PostgreSQL workspace with a browser cache.</div>
      @if (portfolio.quotaExceeded()) {
        <div class="notice warning" role="alert"><span>{{ portfolio.quotaMessage() }}</span> <a class="secondary-button" routerLink="/upgrade" (click)="trackUpgradeClick()">View plans</a></div>
      }
      <form class="card holding-form" (ngSubmit)="addHolding()" novalidate>
        <div class="section-title"><h2>Add a holding</h2><span>{{ portfolio.holdings().length }} holdings</span></div>
        <div class="holding-form-grid">
          <label>Ticker<app-ticker-autocomplete inputId="portfolio-ticker" ariaLabel="Holding ticker" [(value)]="ticker" (valueChange)="formError.set('')" /></label>
          <label>Quantity<input name="quantity" [(ngModel)]="quantity" type="number" min="0.000001" step="any" placeholder="Number of shares you own" required /></label>
          <label>Average cost<input name="averageCost" [(ngModel)]="averageCost" type="number" min="0.0001" step="any" placeholder="Price per share when you bought it" required /><small>Use the ticker's native currency</small></label>
          <label>Purchase date<input name="purchasedAt" [(ngModel)]="purchasedAt" type="date" /></label>
          <label class="holding-note-field">Research note<input name="note" [(ngModel)]="note" maxlength="300" placeholder="Why is this holding in the portfolio?" /></label>
          <button type="submit">Add holding</button>
        </div>
        @if (formError()) { <p class="field-error" role="alert">{{ formError() }}</p> }
      </form>

      @if (loading()) { <div class="loading card" role="status"><span class="spinner"></span><strong>Updating delayed portfolio values...</strong></div> }
      @if (error()) { <div class="error card" role="alert"><p>{{ error() }}</p><button type="button" class="retry-button" (click)="loadSummary()">Try again</button></div> }
      @if (summary(); as data) {
        @if (!data.holdings.length && !portfolio.holdings().length) { <div class="empty card"><h2>No holdings yet</h2><p>Add your first position above. No brokerage credentials are required.</p></div> }
        @if (data.buckets.length) {
          <section class="portfolio-buckets">
            @for (bucket of data.buckets; track bucket.currency) {
              <article class="card bucket-card"><div class="section-title"><h2>{{ bucket.currency }} portfolio</h2><span>{{ bucket.holding_count }} holdings</span></div><strong>{{ money(bucket.market_value, bucket.currency) }}</strong><div class="bucket-metrics"><span>Cost <b>{{ money(bucket.cost_basis, bucket.currency) }}</b></span><span>Unrealized <b [class.negative]="bucket.unrealized_gain < 0">{{ signedMoney(bucket.unrealized_gain, bucket.currency) }} · {{ bucket.gain_percent | percent:'1.1-1' }}</b></span><span>Today <b [class.negative]="bucket.day_change_value < 0">{{ signedMoney(bucket.day_change_value, bucket.currency) }}</b></span></div></article>
            }
          </section>
        }
        @if (data.risk_flags.length) { <section class="card portfolio-risk"><div class="section-title"><h2>Portfolio risk flags</h2><span>Research prompts</span></div><ul>@for (flag of data.risk_flags; track flag) { <li>{{ flag }}</li> }</ul></section> }
        @if (data.holdings.length) {
          <section class="card holdings-table">
            <div class="holdings-meta"><span>Delayed valuation</span><span>Updated {{ data.as_of | date:'medium' }} · {{ data.data_source }}</span></div>
            @for (holding of data.holdings; track holding.id) {
              <article class="holding-row">
                <div class="holding-identity"><a [routerLink]="['/stocks', holding.ticker]"><strong>{{ holding.ticker }}</strong><span>{{ holding.quantity | number:'1.0-6' }} shares · avg {{ money(holding.average_cost, holding.currency) }}</span></a>@if (holding.note) { <small>{{ holding.note }}</small> }</div>
                <div><span>Current value</span><strong>{{ money(holding.market_value, holding.currency) }}</strong><small>{{ money(holding.current_price, holding.currency) }} per share</small></div>
                <div><span>Unrealized</span><strong [class.negative]="holding.unrealized_gain < 0">{{ signedMoney(holding.unrealized_gain, holding.currency) }}</strong><small [class.negative]="holding.gain_percent < 0">{{ holding.gain_percent | percent:'1.1-1' }}</small></div>
                <div><span>Today</span><strong [class.negative]="holding.day_change_value < 0">{{ signedMoney(holding.day_change_value, holding.currency) }}</strong><small>{{ holding.weight_percent | percent:'1.0-0' }} of {{ holding.currency }}</small></div>
                <div class="allocation-cell"><span>Allocation</span><div class="allocation-track"><i [style.width.%]="holding.weight_percent * 100"></i></div></div>
                <button type="button" class="icon-button" [attr.aria-label]="'Remove ' + holding.ticker + ' holding'" (click)="remove(holding.id)">×</button>
              </article>
            }
          </section>
        }
        @if (data.missing_tickers.length) { <div class="notice warning">Current quote data is unavailable for {{ data.missing_tickers.join(', ') }}. Those holdings are excluded from totals.</div> }
        <div class="notice portfolio-disclaimer">{{ data.disclaimer }} Values in different currencies are intentionally not combined.</div>
      }
    </main>`,
})
export class PortfolioComponent {
  private readonly api = inject(StockApiService);
  readonly portfolio = inject(PortfolioService);
  readonly summary = signal<PortfolioSummary | null>(null); readonly loading = signal(false); readonly error = signal(''); readonly formError = signal('');
  ticker = ''; quantity = 0; averageCost = 0; purchasedAt = ''; note = '';

  constructor() { effect(() => { if (this.portfolio.syncState() === 'synced') this.loadSummary(); }); }

  addHolding(): void {
    const tickerError = tickerValidationMessage(this.ticker);
    if (tickerError) { this.formError.set(tickerError); return; }
    if (!Number.isFinite(this.quantity) || this.quantity <= 0) { this.formError.set('Quantity must be greater than zero.'); return; }
    if (!Number.isFinite(this.averageCost) || this.averageCost <= 0) { this.formError.set('Average cost must be greater than zero.'); return; }
    this.portfolio.add({ ticker: normalizeTicker(this.ticker), quantity: this.quantity, averageCost: this.averageCost, purchasedAt: this.purchasedAt, note: this.note });
    this.ticker = ''; this.quantity = 0; this.averageCost = 0; this.purchasedAt = ''; this.note = ''; this.formError.set('');
  }

  remove(id: string): void { this.portfolio.remove(id); }
  loadSummary(): void { this.loading.set(true); this.error.set(''); this.api.getPortfolioSummary().pipe(finalize(() => this.loading.set(false))).subscribe({ next: (summary) => this.summary.set(summary), error: (error) => this.error.set(error.error?.detail ?? 'Portfolio values could not be loaded.') }); }
  trackUpgradeClick(): void {
    this.api.recordMonetizationEvent({
      eventName: 'quota_callout_click',
      source: 'portfolio',
      featureKey: 'portfolio_holding',
    }).subscribe({ error: () => undefined });
  }
  syncLabel(): string { return this.portfolio.syncState() === 'synced' ? 'Portfolio synced.' : this.portfolio.syncState() === 'syncing' ? 'Syncing portfolio...' : this.portfolio.syncState() === 'offline' ? 'Offline mode.' : this.portfolio.syncState() === 'blocked' ? 'Plan limit reached.' : 'Local portfolio.'; }
  money(value: number, currency: string): string { return new Intl.NumberFormat(currency === 'INR' ? 'en-IN' : 'en-US', { style: 'currency', currency, maximumFractionDigits: 2 }).format(value); }
  signedMoney(value: number, currency: string): string { return `${value > 0 ? '+' : ''}${this.money(value, currency)}`; }
}
