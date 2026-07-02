import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../auth/auth.service';
import { MonetizationStatus, SubscriptionPlan, UsageFeature } from '../core/models';
import { StockApiService } from '../core/stock-api.service';
import { TierBadgeComponent } from '../shared/tier-badge.component';

@Component({
  imports: [CommonModule, RouterLink, TierBadgeComponent],
  template: `
    <main class="page upgrade-page">
      <div class="upgrade-heading">
        <p class="eyebrow">MEMBERSHIP</p>
        <h1>Choose your research workspace.</h1>
        <p class="lead">Start free with the core research tools. Paid plans add higher limits and faster workflows without changing the educational focus.</p>
      </div>

      @if (loading()) {
        <section class="status-card" role="status">
          <span class="spinner" aria-hidden="true"></span>
          <strong>Loading plan details...</strong>
        </section>
      }
      @if (error()) {
        <section class="status-card error" role="alert">
          <p>{{ error() }}</p>
          <button type="button" class="secondary-button" (click)="loadStatus()">Try again</button>
        </section>
      }
      @if (status(); as state) {
        <section class="status-card">
          <div>
            <p class="eyebrow">CURRENT PLAN</p>
            <h2>{{ planName(state.plan) }}</h2>
            <p>{{ statusText(state) }}</p>
          </div>
          <div class="status-actions">
            @if (!state.authenticated) {
              <a routerLink="/login" class="secondary-link">Sign in before checkout</a>
            }
            @if (state.subscription?.currentPeriodEnd) {
              <span>Renews through {{ state.subscription?.currentPeriodEnd | date:'mediumDate' }}</span>
            }
          </div>
        </section>

        <section class="usage-section" aria-label="Current usage">
          <div class="section-title"><h2>Today&apos;s limits</h2><span>{{ planName(state.plan) }}</span></div>
          <div class="usage-grid">
            @for (item of visibleUsage(); track item.featureKey) {
              <article class="usage-item" [class.blocked]="!item.allowed">
                <span>{{ item.label }}</span>
                <strong>{{ usageValue(item) }}</strong>
                <small>{{ usageDetail(item) }}</small>
              </article>
            }
          </div>
        </section>
      }

      <section class="pricing-grid" aria-label="StockAnalyzer plans">
        @for (plan of plans(); track plan.key) {
          <article class="plan-card" [class.pro-plan]="plan.key !== 'free'" [class.current]="status()?.plan === plan.key">
            <div class="plan-heading">
              <app-tier-badge [tier]="tierFor(plan)" />
              <span>{{ plan.key === 'free' ? 'Current foundation' : plan.description }}</span>
            </div>
            <h2>{{ plan.name }}</h2>
            <p class="price"><strong>{{ plan.priceLabel }}</strong><span>{{ plan.key === 'free' ? 'forever' : 'paid plan' }}</span></p>
            <ul>
              @for (limit of plan.limits; track limit.featureKey) {
                <li>{{ limitText(limit) }}</li>
              }
            </ul>
            @if (plan.key === 'free') {
              <button type="button" class="secondary-button" disabled>Included</button>
            } @else if (status()?.plan === plan.key) {
              <button type="button" class="secondary-button" disabled>Active plan</button>
            } @else if (!auth.authenticated()) {
              <a routerLink="/login" class="plan-link">Sign in to upgrade</a>
            } @else {
              <button type="button" [disabled]="checkoutLoading() === plan.key" (click)="startCheckout(plan)">
                {{ checkoutLoading() === plan.key ? 'Starting checkout...' : 'Upgrade to ' + plan.name }}
              </button>
            }
          </article>
        }
      </section>

      @if (message()) { <p class="upgrade-note" role="status">{{ message() }}</p> }
      <p class="upgrade-note">Manual checkout is enabled for local testing. Paid access starts only after the backend receives a provider confirmation webhook.</p>
    </main>
  `,
  styles: [`
    .upgrade-heading { max-width: 760px; }
    .upgrade-heading h1 { font-size: clamp(2.7rem, 6vw, 4.7rem); }
    .status-card { display: flex; align-items: center; justify-content: space-between; gap: 20px; max-width: 980px; margin-top: 34px; padding: 22px; border: 1px solid var(--border); border-radius: 12px; background: var(--surface); }
    .status-card h2 { margin: 0 0 6px; font-size: 1.6rem; }
    .status-card p { margin: 0; color: var(--muted); font-size: .78rem; line-height: 1.5; }
    .status-card.error { border-color: rgba(255,111,111,.4); }
    .status-actions { display: grid; gap: 8px; justify-items: end; color: var(--muted); font-size: .74rem; }
    .secondary-link, .plan-link { display: inline-flex; align-items: center; justify-content: center; min-height: 42px; padding: 0 18px; border: 1px solid var(--border); border-radius: 999px; color: var(--text); text-decoration: none; font-weight: 800; font-size: .72rem; }
    .usage-section { max-width: 980px; margin-top: 28px; }
    .usage-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; }
    .usage-item { display: grid; gap: 8px; min-height: 112px; padding: 18px; border: 1px solid var(--border); border-radius: 10px; background: rgba(255,255,255,.025); }
    .usage-item span { color: var(--muted); font-size: .68rem; text-transform: uppercase; }
    .usage-item strong { font-size: 1.35rem; }
    .usage-item small { color: #758179; font-size: .68rem; line-height: 1.4; }
    .usage-item.blocked { border-color: rgba(255,111,111,.35); }
    .pricing-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 16px; max-width: 1180px; margin-top: 48px; }
    .plan-card { position: relative; padding: 30px; border: 1px solid var(--border); border-radius: 14px; background: linear-gradient(145deg, rgba(255,255,255,.025), transparent 55%), var(--surface); box-shadow: 0 18px 50px rgba(0,0,0,.14); }
    .pro-plan { border-color: rgba(88,235,166,.28); background: radial-gradient(circle at 90% 0, rgba(88,235,166,.11), transparent 32%), var(--surface); }
    .plan-card.current { outline: 2px solid rgba(88,235,166,.3); }
    .plan-heading { display: flex; align-items: center; justify-content: space-between; gap: 12px; color: var(--muted); font-size: .68rem; }
    h2 { margin: 28px 0 8px; font-size: 1.45rem; }
    .price { display: grid; gap: 4px; min-height: 64px; margin: 0; color: var(--muted); }
    .price strong { color: var(--text); font: 800 1.65rem Manrope, sans-serif; line-height: 1.05; }
    .price span { font-size: .72rem; }
    ul { display: grid; gap: 13px; margin: 30px 0; padding: 0; list-style: none; }
    li { position: relative; padding-left: 22px; color: #bdc7c0; font-size: .82rem; line-height: 1.45; }
    li::before { position: absolute; left: 0; color: var(--accent); content: '✓'; }
    .plan-card > button, .plan-card > .plan-link { width: 100%; }
    .coming-soon { margin: 12px 0 0; color: var(--muted); font-size: .7rem; text-align: center; }
    .upgrade-note { max-width: 980px; margin: 18px 0 0; color: #667269; font-size: .68rem; text-align: center; }
    @media (max-width: 960px) { .pricing-grid, .usage-grid { grid-template-columns: 1fr; } .status-card { align-items: flex-start; flex-direction: column; } .status-actions { justify-items: start; } }
    @media (max-width: 760px) { .pricing-grid { margin-top: 32px; } .plan-card { padding: 24px; } }
  `],
})
export class UpgradeComponent implements OnInit {
  private readonly api = inject(StockApiService);
  readonly auth = inject(AuthService);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly message = signal('');
  readonly status = signal<MonetizationStatus | null>(null);
  readonly checkoutLoading = signal('');
  readonly plans = computed(() => this.status()?.plans ?? []);
  readonly visibleUsage = computed(() => this.status()?.usage ?? []);

  ngOnInit(): void {
    this.loadStatus();
  }

  loadStatus(): void {
    this.loading.set(true);
    this.error.set('');
    this.api.getMonetizationStatus().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (status) => this.status.set(status),
      error: (error) => this.error.set(error.error?.detail ?? 'Plan details could not be loaded.'),
    });
  }

  startCheckout(plan: SubscriptionPlan): void {
    if (plan.key === 'free' || !this.auth.authenticated()) return;
    this.checkoutLoading.set(plan.key);
    this.message.set('');
    const origin = window.location.origin;
    this.api.startCheckout(plan.key, `${origin}/upgrade?checkout=success`, `${origin}/upgrade?checkout=cancelled`)
      .pipe(finalize(() => this.checkoutLoading.set('')))
      .subscribe({
        next: (response) => {
          this.message.set(response.message);
          window.location.assign(response.checkoutUrl);
        },
        error: (error) => this.message.set(error.error?.detail ?? 'Checkout could not be started.'),
      });
  }

  planName(plan: string): string {
    return plan.charAt(0).toUpperCase() + plan.slice(1);
  }

  tierFor(plan: SubscriptionPlan): 'Free' | 'Pro' | 'Power' {
    return plan.key === 'power' ? 'Power' : plan.key === 'pro' ? 'Pro' : 'Free';
  }

  statusText(state: MonetizationStatus): string {
    if (!state.authenticated) return 'You are using an anonymous Free workspace. Sign in before starting checkout.';
    if (!state.subscription) return 'No active subscription yet. Paid access starts after provider confirmation.';
    if (state.subscription.status === 'active') return `${this.planName(state.subscription.plan)} is active.`;
    if (state.subscription.status === 'pending') return `${this.planName(state.subscription.plan)} checkout is pending provider confirmation.`;
    if (state.subscription.status === 'past_due') return `${this.planName(state.subscription.plan)} payment is past due.`;
    return `${this.planName(state.subscription.plan)} subscription is canceled.`;
  }

  usageValue(item: UsageFeature): string {
    if (item.dailyLimit !== null) return `${item.usedToday}/${item.dailyLimit}`;
    if (item.storedLimit !== null) return `${item.storedLimit}`;
    return 'Included';
  }

  usageDetail(item: UsageFeature): string {
    if (item.dailyLimit !== null) {
      return `${item.remainingToday ?? 0} ${item.unit}${(item.remainingToday ?? 0) === 1 ? '' : 's'} remaining today`;
    }
    if (item.storedLimit !== null) return `Stored limit: ${item.storedLimit} ${item.unit}${item.storedLimit === 1 ? '' : 's'}`;
    return 'Available in this plan';
  }

  limitText(limit: SubscriptionPlan['limits'][number]): string {
    if (limit.dailyLimit !== null) return `${limit.dailyLimit} ${limit.label.toLowerCase()} per day`;
    if (limit.storedLimit !== null) return `${limit.storedLimit} ${limit.label.toLowerCase()}`;
    return limit.label;
  }
}
