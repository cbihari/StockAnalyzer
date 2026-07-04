import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { MonetizationFunnelReport } from '../core/models';
import { StockApiService } from '../core/stock-api.service';

@Component({
  imports: [CommonModule, FormsModule],
  template: `
    <main class="page monetization-funnel-page">
      <div class="portfolio-heading">
        <div><p class="eyebrow">ADMIN</p><h1>Monetization funnel</h1><p class="lead">Aggregate upgrade intent, quota prompts, and checkout events without exposing ticker searches or personal notes.</p></div>
        <form class="history-filters card" (ngSubmit)="load()">
          <label>Window<select name="days" [(ngModel)]="days"><option [ngValue]="7">7 days</option><option [ngValue]="30">30 days</option><option [ngValue]="90">90 days</option></select></label>
          <button type="submit" [disabled]="loading()">{{ loading() ? 'Loading...' : 'Refresh' }}</button>
          <button type="button" class="secondary-button" [disabled]="!report() || exporting()" (click)="exportCsv()">{{ exporting() ? 'Exporting...' : 'Export CSV' }}</button>
        </form>
      </div>

      @if (loading()) { <div class="loading card" role="status"><span class="spinner"></span> Loading monetization funnel...</div> }
      @if (error()) { <div class="empty card" role="alert"><h2>Funnel report unavailable</h2><p>{{ error() }}</p></div> }
      @if (exportMessage()) { <div class="notice" role="status">{{ exportMessage() }}</div> }
      @if (report(); as data) {
        <section class="history-summary-grid">
          <article class="card"><span>TOTAL EVENTS</span><strong>{{ data.totalEvents }}</strong><p>{{ data.from | date:'mediumDate':'UTC' }} to {{ data.to | date:'mediumDate':'UTC' }}</p></article>
          @for (item of data.events.slice(0, 2); track item.eventName) {
            <article class="card"><span>{{ label(item.eventName) }}</span><strong>{{ item.count }}</strong><p>Recorded events</p></article>
          }
        </section>

        @if (dailyTotals().length) {
          <section class="card funnel-trend">
            <div class="section-title"><h2>Daily trend</h2><span>{{ data.from | date:'mediumDate':'UTC' }} - {{ data.to | date:'mediumDate':'UTC' }}</span></div>
            <div class="trend-list">
              @for (item of dailyTotals(); track item.date) {
                <div class="trend-row"><span>{{ item.date | date:'MMM d':'UTC' }}</span><div class="trend-track"><i [style.width.%]="item.count * 100 / maxDailyTotal()"></i></div><strong>{{ item.count }}</strong></div>
              }
            </div>
          </section>
        }

        <div class="table-card card"><table><thead><tr><th>Event</th><th>Source</th><th>Feature</th><th>Plan</th><th>Count</th></tr></thead><tbody>
          @for (item of data.breakdown; track item.eventName + item.source + item.featureKey + item.planKey) {
            <tr><td>{{ label(item.eventName) }}</td><td>{{ item.source }}</td><td>{{ item.featureKey ?? 'n/a' }}</td><td>{{ item.planKey ?? 'n/a' }}</td><td>{{ item.count }}</td></tr>
          }
        </tbody></table></div>
        @if (!data.breakdown.length) { <div class="empty card"><h2>No funnel events yet</h2><p>Quota prompts and checkout actions will appear here after users interact with paid limits.</p></div> }
      }
    </main>
  `,
  styles: [`
    .funnel-trend { margin: 22px 0; }
    .trend-list { display: grid; gap: 10px; }
    .trend-row { display: grid; grid-template-columns: 82px minmax(120px, 1fr) 44px; align-items: center; gap: 12px; color: var(--muted); font-size: .74rem; }
    .trend-row strong { color: var(--text); text-align: right; }
    .trend-track { height: 10px; overflow: hidden; border-radius: 999px; background: rgba(255,255,255,.06); }
    .trend-track i { display: block; min-width: 3px; height: 100%; border-radius: inherit; background: var(--accent); }
    @media (max-width: 640px) { .trend-row { grid-template-columns: 70px 1fr 36px; gap: 8px; } }
  `],
})
export class MonetizationFunnelComponent implements OnInit {
  private readonly api = inject(StockApiService);
  readonly report = signal<MonetizationFunnelReport | null>(null);
  readonly loading = signal(true);
  readonly exporting = signal(false);
  readonly error = signal('');
  readonly exportMessage = signal('');
  readonly dailyTotals = computed(() => {
    const report = this.report();
    if (!report) return [];
    const totals = new Map<string, number>();
    for (const item of report.daily) totals.set(item.date, (totals.get(item.date) ?? 0) + item.count);
    return [...totals.entries()].map(([date, count]) => ({ date, count })).sort((a, b) => a.date.localeCompare(b.date));
  });
  readonly maxDailyTotal = computed(() => Math.max(1, ...this.dailyTotals().map((item) => item.count)));
  days = 30;

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.api.getMonetizationFunnel(this.days).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (report) => this.report.set(report),
      error: (error) => {
        this.report.set(null);
        this.error.set(error.status === 404
          ? 'Enable MONETIZATION_ADMIN_ENABLED on the API to view this page.'
          : 'Monetization funnel could not be loaded.');
      },
    });
  }

  exportCsv(): void {
    this.exporting.set(true);
    this.exportMessage.set('');
    this.api.exportMonetizationFunnelCsv(this.days).pipe(finalize(() => this.exporting.set(false))).subscribe({
      next: (response) => {
        this.downloadCsv(
          response.body ?? new Blob([], { type: 'text/csv;charset=utf-8' }),
          this.exportFileName(response.headers.get('content-disposition')));
        this.exportMessage.set('Monetization funnel CSV exported.');
      },
      error: (error) => this.exportMessage.set(error.status === 404
        ? 'Enable MONETIZATION_ADMIN_ENABLED on the API before exporting.'
        : 'Monetization funnel CSV could not be exported.'),
    });
  }

  label(value: string): string {
    return value.split('_').map((part) => part.charAt(0).toUpperCase() + part.slice(1)).join(' ');
  }

  private downloadCsv(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  }

  private exportFileName(contentDisposition: string | null): string {
    const match = contentDisposition?.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i);
    return match?.[1] ? decodeURIComponent(match[1]) : `stockanalyzer-monetization-funnel-${new Date().toISOString().slice(0, 10)}.csv`;
  }
}
