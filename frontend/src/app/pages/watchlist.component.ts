import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { MarketInstrument } from '../core/models';
import { StockApiService } from '../core/stock-api.service';
import { WatchlistItem, WatchlistService } from '../core/watchlist.service';
import { SparklineComponent } from '../shared/sparkline.component';
import { AlertDraft, AlertFrequency, AlertService, AlertType } from '../core/alert.service';

@Component({
  imports: [CommonModule, FormsModule, RouterLink, SparklineComponent],
  template: `
    <main class="page">
      <div class="watchlist-heading">
        <div><p class="eyebrow">DAILY RESEARCH LIST</p><h1>Your watchlist</h1><p class="lead">A compact delayed snapshot of stocks you want to research again.</p></div>
        <a class="secondary-button link-button" routerLink="/search">Add stocks</a>
      </div>
      <div class="watchlist-note notice">Saved in this browser for now. Account sync and alerts will plug into the same watchlist contract later.</div>
      @if (availableTags().length) {
        <div class="watchlist-filters" aria-label="Filter watchlist by tag"><span>Filter</span><button type="button" [class.active]="!activeTag()" (click)="activeTag.set('')">All</button>@for (tag of availableTags(); track tag) { <button type="button" [class.active]="activeTag() === tag" (click)="activeTag.set(tag)">#{{ tag }}</button> }</div>
      }
      @if (loading()) { <div class="loading card" role="status"><span class="spinner" aria-hidden="true"></span> Refreshing watchlist quotes...</div> }
      @if (error()) { <div class="error card" role="alert">{{ error() }} <button type="button" class="retry-button" (click)="load()">Try again</button></div> }
      @if (!loading() && !watchlist.tickers().length) { <div class="empty card"><h2>Your watchlist is empty</h2><p>Open a stock analysis and select “Add to watchlist”.</p><a class="link-button" routerLink="/search">Research stocks</a></div> }
      @if (quotes().length) {
        <section class="watchlist-table card">
          <div class="watchlist-meta"><span>{{ quotes().length }} tracked stocks</span><span>Updated {{ asOf() | date:'mediumTime' }} · {{ dataSource() }}</span></div>
          @for (quote of filteredQuotes(); track quote.symbol) {
            <article class="watchlist-item">
              <div class="watchlist-row">
                <div class="watchlist-identity"><a [routerLink]="['/stocks', quote.symbol]"><strong>{{ quote.symbol }}</strong><span>Open research brief</span></a>@if (itemFor(quote.symbol); as item) { <div class="watchlist-tags">@for (tag of item.tags; track tag) { <button type="button" (click)="activeTag.set(tag)">#{{ tag }}</button> }</div> }</div>
                <app-sparkline [values]="quote.sparkline" [negative]="quote.change < 0" />
                <div class="watchlist-price"><strong>{{ quote.symbol.endsWith('.NS') || quote.symbol.endsWith('.BO') ? '₹' : '$' }}{{ quote.price | number:'1.2-2' }}</strong><span [class.negative]="quote.change < 0">{{ quote.change >= 0 ? '+' : '' }}{{ quote.change_percent | percent:'1.2-2' }}</span></div>
                <div class="watchlist-session"><span>Range</span><strong>{{ quote.day_low | number:'1.2-2' }} – {{ quote.day_high | number:'1.2-2' }}</strong></div>
                <div class="watchlist-actions"><button type="button" class="note-button" [attr.aria-label]="'Manage alerts for ' + quote.symbol" (click)="openAlertEditor(quote.symbol)">{{ alerts.rulesFor(quote.symbol).length ? alerts.rulesFor(quote.symbol).length + ' Alert' : '+ Alert' }}</button><button type="button" class="note-button" [attr.aria-label]="'Edit note for ' + quote.symbol" (click)="openEditor(quote.symbol)">{{ itemFor(quote.symbol)?.note ? 'Note' : '+ Note' }}</button><button type="button" class="icon-button" [attr.aria-label]="'Remove ' + quote.symbol + ' from watchlist'" (click)="remove(quote.symbol)">×</button></div>
              </div>
              @if (itemFor(quote.symbol)?.note && editingTicker() !== quote.symbol) { <p class="watchlist-thesis"><span>RESEARCH NOTE</span>{{ itemFor(quote.symbol)?.note }}</p> }
              @if (editingTicker() === quote.symbol) {
                <form class="watchlist-editor" (ngSubmit)="saveEditor()">
                  <label [for]="'note-' + quote.symbol">Research note<textarea [id]="'note-' + quote.symbol" name="note" [(ngModel)]="draftNote" maxlength="500" placeholder="What are you watching, and what could change your view?"></textarea><small>{{ draftNote.length }}/500</small></label>
                  <label [for]="'tags-' + quote.symbol">Tags<input [id]="'tags-' + quote.symbol" name="tags" [(ngModel)]="draftTags" maxlength="100" placeholder="earnings, momentum, long term" /><small>Comma-separated · maximum 5</small></label>
                  <div><button type="button" class="secondary-button" (click)="closeEditor()">Cancel</button><button type="submit">Save note</button></div>
                </form>
              }
              @if (alertTicker() === quote.symbol) {
                <section class="alert-editor">
                  <div class="alert-editor-heading"><div><span>IN-APP ALERTS</span><strong>{{ quote.symbol }}</strong></div><button type="button" class="icon-button" aria-label="Close alert editor" (click)="closeAlertEditor()">×</button></div>
                  @if (alerts.rulesFor(quote.symbol).length) { <div class="alert-rule-list">@for (rule of alerts.rulesFor(quote.symbol); track rule.id) { <div><button type="button" class="alert-state" [class.disabled]="!rule.enabled" (click)="alerts.toggle(rule.id)">{{ rule.enabled ? 'ON' : 'OFF' }}</button><span><strong>{{ alerts.describe(rule) }}</strong><small>{{ rule.frequency === 'once' ? 'Once' : 'Daily' }} · {{ rule.cooldownHours }}h cooldown · In-app</small></span><button type="button" class="alert-delete" [attr.aria-label]="'Delete alert ' + alerts.describe(rule)" (click)="alerts.remove(rule.id)">Delete</button></div> }</div> }
                  <form class="alert-form" (ngSubmit)="saveAlert(quote.symbol)">
                    <label>Condition<select name="alertType" [(ngModel)]="alertType"><option value="price_above">Price above</option><option value="price_below">Price below</option><option value="daily_move">Daily move reaches</option></select></label>
                    <label>Threshold<input name="alertThreshold" [(ngModel)]="alertThreshold" type="number" min="0.01" step="0.01" required /><small>{{ alertType === 'daily_move' ? 'Percent, absolute move' : 'Price in ticker currency' }}</small></label>
                    <label>Frequency<select name="alertFrequency" [(ngModel)]="alertFrequency"><option value="once">Once</option><option value="daily">Daily</option></select></label>
                    <label>Cooldown<select name="alertCooldown" [(ngModel)]="alertCooldown"><option [ngValue]="6">6 hours</option><option [ngValue]="12">12 hours</option><option [ngValue]="24">24 hours</option><option [ngValue]="72">3 days</option></select></label>
                    <label>Quiet from<input name="quietStart" [(ngModel)]="quietStart" type="time" /></label><label>Quiet until<input name="quietEnd" [(ngModel)]="quietEnd" type="time" /></label>
                    <button type="submit" [disabled]="alertThreshold <= 0">Create alert</button>
                  </form>
                  <p class="alert-help">Evaluated when this browser refreshes watchlist quotes. Email and background delivery are not active yet.</p>
                </section>
              }
            </article>
          }
          @if (!filteredQuotes().length) { <div class="watchlist-filter-empty">No stocks use the <strong>#{{ activeTag() }}</strong> tag.</div> }
        </section>
      }
    </main>`,
})
export class WatchlistComponent implements OnInit {
  private readonly api = inject(StockApiService);
  readonly watchlist = inject(WatchlistService);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly quotes = signal<MarketInstrument[]>([]);
  readonly asOf = signal('');
  readonly dataSource = signal('');
  readonly activeTag = signal('');
  readonly editingTicker = signal('');
  readonly alertTicker = signal('');
  readonly alerts = inject(AlertService);
  draftNote = '';
  draftTags = '';
  alertType: AlertType = 'price_above';
  alertThreshold = 0;
  alertFrequency: AlertFrequency = 'once';
  alertCooldown = 24;
  quietStart = '22:00';
  quietEnd = '07:00';

  ngOnInit(): void { this.load(); }

  load(): void {
    const tickers = this.watchlist.tickers();
    if (!tickers.length) { this.quotes.set([]); return; }
    this.loading.set(true); this.error.set('');
    this.api.getStockQuotes(tickers).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (response) => { this.quotes.set(response.quotes); this.asOf.set(response.as_of); this.dataSource.set(response.data_source); this.alerts.evaluate(response.quotes, response.as_of); },
      error: (error) => this.error.set(error.error?.detail ?? 'Watchlist quotes could not be loaded.'),
    });
  }

  remove(ticker: string): void { this.watchlist.remove(ticker); this.quotes.update((items) => items.filter((item) => item.symbol !== ticker)); }
  itemFor(ticker: string): WatchlistItem | undefined { return this.watchlist.get(ticker); }
  availableTags(): string[] { return [...new Set(this.watchlist.items().flatMap((item) => item.tags))].sort(); }
  filteredQuotes(): MarketInstrument[] { const tag = this.activeTag(); return tag ? this.quotes().filter((quote) => this.itemFor(quote.symbol)?.tags.includes(tag)) : this.quotes(); }
  openEditor(ticker: string): void { const item = this.itemFor(ticker); this.editingTicker.set(ticker); this.draftNote = item?.note ?? ''; this.draftTags = item?.tags.join(', ') ?? ''; }
  closeEditor(): void { this.editingTicker.set(''); this.draftNote = ''; this.draftTags = ''; }
  saveEditor(): void { const ticker = this.editingTicker(); if (!ticker) return; this.watchlist.updateDetails(ticker, this.draftNote, this.draftTags.split(',')); this.closeEditor(); }
  openAlertEditor(ticker: string): void { this.alertTicker.set(this.alertTicker() === ticker ? '' : ticker); this.editingTicker.set(''); const quote = this.quotes().find((item) => item.symbol === ticker); this.alertThreshold = quote?.price ?? 0; }
  closeAlertEditor(): void { this.alertTicker.set(''); }
  saveAlert(ticker: string): void {
    const draft: AlertDraft = { type: this.alertType, threshold: this.alertThreshold, frequency: this.alertFrequency, cooldownHours: this.alertCooldown, quietStart: this.quietStart, quietEnd: this.quietEnd };
    this.alerts.add(ticker, draft); this.alertThreshold = this.quotes().find((item) => item.symbol === ticker)?.price ?? 0;
  }
}
