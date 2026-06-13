import { Component, signal } from '@angular/core';
import { TierBadgeComponent } from '../shared/tier-badge.component';

@Component({
  imports: [TierBadgeComponent],
  template: `
    <main class="page upgrade-page">
      <div class="upgrade-heading">
        <p class="eyebrow">MEMBERSHIP</p>
        <h1>Choose your research workspace.</h1>
        <p class="lead">Start free with the core research tools. Pro adds higher limits and faster workflows without changing the educational focus.</p>
      </div>

      <section class="pricing-grid" aria-label="StockAnalyzer plans">
        <article class="plan-card">
          <div class="plan-heading"><app-tier-badge tier="Free" /><span>Current foundation</span></div>
          <h2>Free</h2>
          <p class="price"><strong>₹0</strong><span>forever</span></p>
          <ul>
            <li>5 watchlist stocks</li>
            <li>3 AI questions per day</li>
            <li>Daily prediction refresh</li>
            <li>2-stock comparison</li>
            <li>Full Learning Center</li>
          </ul>
          <button type="button" class="secondary-button" disabled>Included</button>
        </article>

        <article class="plan-card pro-plan">
          <div class="plan-heading"><app-tier-badge tier="Pro" /><span>For deeper research</span></div>
          <h2>Pro</h2>
          <p class="price"><strong>₹149</strong><span>/ month · ₹1499 / year</span></p>
          <ul>
            <li>50 watchlist stocks</li>
            <li>Unlimited AI questions</li>
            <li>On-demand prediction refresh</li>
            <li>3-stock comparison</li>
            <li>Price alerts</li>
            <li>CSV export</li>
          </ul>
          <button type="button" (click)="requestUpgrade()">Upgrade to Pro</button>
          @if (message()) { <p class="coming-soon" role="status">{{ message() }}</p> }
        </article>
      </section>

      <p class="upgrade-note">No payment is collected yet. Existing features and access remain unchanged.</p>
    </main>
  `,
  styles: [`
    .upgrade-heading { max-width: 760px; }
    .upgrade-heading h1 { font-size: clamp(2.7rem, 6vw, 4.7rem); }
    .pricing-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; max-width: 980px; margin-top: 48px; }
    .plan-card { position: relative; padding: 30px; border: 1px solid var(--border); border-radius: 14px; background: linear-gradient(145deg, rgba(255,255,255,.025), transparent 55%), var(--surface); box-shadow: 0 18px 50px rgba(0,0,0,.14); }
    .pro-plan { border-color: rgba(88,235,166,.28); background: radial-gradient(circle at 90% 0, rgba(88,235,166,.11), transparent 32%), var(--surface); }
    .plan-heading { display: flex; align-items: center; justify-content: space-between; gap: 12px; color: var(--muted); font-size: .68rem; }
    h2 { margin: 28px 0 8px; font-size: 1.45rem; }
    .price { display: flex; align-items: baseline; gap: 8px; margin: 0; color: var(--muted); }
    .price strong { color: var(--text); font: 800 2.5rem Manrope, sans-serif; }
    .price span { font-size: .72rem; }
    ul { display: grid; gap: 13px; margin: 30px 0; padding: 0; list-style: none; }
    li { position: relative; padding-left: 22px; color: #bdc7c0; font-size: .82rem; line-height: 1.45; }
    li::before { position: absolute; left: 0; color: var(--accent); content: '✓'; }
    .plan-card > button { width: 100%; }
    .coming-soon { margin: 12px 0 0; color: var(--muted); font-size: .7rem; text-align: center; }
    .upgrade-note { max-width: 980px; margin: 18px 0 0; color: #667269; font-size: .68rem; text-align: center; }
    @media (max-width: 760px) { .pricing-grid { grid-template-columns: 1fr; margin-top: 32px; } .plan-card { padding: 24px; } }
  `],
})
export class UpgradeComponent {
  readonly message = signal('');

  requestUpgrade(): void {
    // TODO: Connect this action to Razorpay when payment integration is implemented.
    this.message.set('Pro checkout is coming soon. No payment was started.');
  }
}
