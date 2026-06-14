import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { AffiliateClickStat } from '../core/models';
import { StockApiService } from '../core/stock-api.service';

@Component({
  imports: [CommonModule],
  template: `
    <main class="page affiliate-stats-page">
      <p class="eyebrow">ADMIN</p>
      <h1>Affiliate clicks</h1>
      <p class="lead">Read-only referral activity grouped by broker and UTC date.</p>

      @if (loading()) { <div class="loading card" role="status"><span class="spinner"></span> Loading affiliate stats...</div> }
      @if (error()) { <div class="empty card" role="alert"><h2>Affiliate stats unavailable</h2><p>{{ error() }}</p></div> }
      @if (!loading() && !error()) {
        <div class="table-card card"><table><thead><tr><th>Date</th><th>Broker</th><th>Clicks</th></tr></thead><tbody>
          @for (item of stats(); track item.date + item.broker) { <tr><td>{{ item.date | date:'mediumDate':'UTC' }}</td><td>{{ item.broker }}</td><td>{{ item.clicks }}</td></tr> }
        </tbody></table></div>
        @if (!stats().length) { <div class="empty card"><h2>No affiliate clicks yet</h2><p>Tracked broker referrals will appear here.</p></div> }
      }
    </main>
  `,
})
export class AffiliateStatsComponent implements OnInit {
  private readonly api = inject(StockApiService);
  readonly stats = signal<AffiliateClickStat[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit(): void {
    this.api.getAffiliateStats().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (stats) => this.stats.set(stats),
      error: (error) => this.error.set(error.status === 404
        ? 'Enable AFFILIATE_ADMIN_ENABLED on the API to view this page.'
        : 'Affiliate statistics could not be loaded.'),
    });
  }
}
