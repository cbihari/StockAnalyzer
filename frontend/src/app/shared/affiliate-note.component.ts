import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { AffiliatePartner } from '../core/models';
import { StockApiService } from '../core/stock-api.service';

@Component({
  selector: 'app-affiliate-note',
  template: `
    @if (visible()) {
      <aside class="affiliate-note" aria-label="Affiliate disclosure">
        <div>
          <p>This analysis is for education only and isn't investment advice. Open a free demat account with our partner broker — StockAnalyzer may earn a referral fee at no extra cost to you. <a href="#" (click)="$event.preventDefault()">Learn more</a></p>
          @if (partners().length) {
            <div class="partner-links">
              @for (partner of partners(); track partner.name) {
                <a [href]="partnerUrl(partner)" target="_blank" rel="noopener noreferrer sponsored" (click)="track(partner.name)"><span>{{ partner.logo }}</span>Open with {{ partner.name }}</a>
              }
            </div>
          }
        </div>
        <button type="button" aria-label="Dismiss affiliate disclosure" (click)="visible.set(false)">×</button>
      </aside>
    }
  `,
  styles: [`
    :host { display: block; }
    .affiliate-note { display: flex; align-items: start; justify-content: space-between; gap: 16px; margin-top: 18px; padding: 15px 2px 0; border-top: 1px solid #202a24; }
    p { max-width: 820px; margin: 0; color: #6f7b73; font-size: .68rem; line-height: 1.6; }
    p a { color: #8fa69a; text-decoration: underline; text-underline-offset: 3px; }
    .partner-links { display: flex; flex-wrap: wrap; gap: 7px; margin-top: 10px; }
    .partner-links a { display: inline-flex; align-items: center; gap: 6px; padding: 6px 9px; border: 1px solid #273129; border-radius: 7px; color: #8f9b93; background: rgba(255,255,255,.018); font-size: .64rem; font-weight: 700; text-decoration: none; }
    .partner-links a:hover, .partner-links a:focus-visible { border-color: #3a4a40; color: #bdc8c0; outline: none; }
    .partner-links span { display: grid; place-items: center; width: 16px; height: 16px; border-radius: 5px; color: #90a199; background: #18211b; font-size: .55rem; }
    button { flex: none; padding: 0 3px; border: 0; color: #667269; background: transparent; font-size: 1rem; line-height: 1; }
    button:hover, button:focus-visible { color: #aebbb3; outline: none; }
  `],
})
export class AffiliateNoteComponent implements OnInit {
  private readonly api = inject(StockApiService);
  @Input() ticker = '';
  readonly visible = signal(true);
  readonly partners = signal<AffiliatePartner[]>([]);

  ngOnInit(): void {
    this.api.getAffiliatePartners().subscribe({
      next: (partners) => this.partners.set(partners),
      error: () => this.partners.set([]),
    });
  }

  partnerUrl(partner: AffiliatePartner): string {
    try {
      const url = new URL(partner.url);
      url.searchParams.set('ref', 'stockanalyzer');
      url.searchParams.set('utm_source', 'research');
      return url.toString();
    } catch { return '#'; }
  }

  track(broker: string): void {
    this.api.trackAffiliateClick(broker, this.ticker).subscribe({ error: () => undefined });
  }
}
