import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TickerAutocompleteComponent } from '../shared/ticker-autocomplete.component';
import { normalizeTicker, tickerValidationMessage } from '../core/ticker-validation';

@Component({
  imports: [RouterLink, TickerAutocompleteComponent],
  template: `
    <main class="landing-page">
      <section class="hero-section">
        <div class="hero-copy">
          <p class="eyebrow">EXPLAINABLE AI STOCK RESEARCH</p>
          <h1>See the signal.<br /><span>Understand the evidence.</span></h1>
          <p class="hero-lead">Research Indian and US stocks with technical context, ticker-specific model estimates, visible uncertainty, and practical explanations.</p>
          <form class="hero-search" (submit)="analyze($event)">
            <app-ticker-autocomplete inputId="hero-ticker" ariaLabel="Search ticker or company" placeholder="Search AAPL, Reliance, TCS..." [(value)]="ticker" (submitted)="openTicker()" />
            <button type="submit">Research stock <span>→</span></button>
          </form>
          @if (error) { <p class="field-error">{{ error }}</p> }
          <div class="hero-trust"><span class="trust-dot"></span> Educational research, not investment advice <span>·</span> India + US coverage</div>
        </div>
        <article class="research-preview">
          <div class="preview-glow"></div>
          <header><div><span class="preview-symbol">RELIANCE.NS</span><small>Reliance Industries</small></div><span class="live-pill">RESEARCH BRIEF</span></header>
          <div class="preview-price"><strong>₹1,405.30</strong><span>+1.24% today</span></div>
          <div class="preview-chart" aria-hidden="true"><i></i><i></i><i></i><i></i><i></i><i></i><i></i><i></i><i></i><i></i><svg viewBox="0 0 500 100" preserveAspectRatio="none"><path d="M0,82 C45,78 52,62 96,69 S155,84 195,55 S250,62 290,42 S350,49 385,25 S450,38 500,12" /></svg></div>
          <div class="preview-grid"><div><span>MODEL ESTIMATE</span><strong class="positive">UP</strong><small>68% confidence</small></div><div><span>RISK</span><strong>MEDIUM</strong><small>Signals mostly aligned</small></div></div>
          <div class="preview-signal"><span>✓</span><div><strong>Trend supports the estimate</strong><small>EMA20 is above EMA50</small></div></div>
          <div class="preview-signal conflict"><span>!</span><div><strong>Volume is not confirming</strong><small>Participation remains below average</small></div></div>
        </article>
      </section>

      <section class="popular-strip"><span>EXPLORE</span>@for (stock of popular; track stock.symbol) { <a [routerLink]="['/stocks', stock.symbol]"><strong>{{ stock.symbol }}</strong><small>{{ stock.market }}</small></a> }</section>

      <section class="value-section">
        <div><p class="eyebrow">A BETTER RESEARCH LOOP</p><h2>One workspace. More honest answers.</h2></div>
        <div class="value-grid"><article><span>01</span><h3>Structured evidence</h3><p>Supporting, conflicting, and neutral signals are separated so uncertainty stays visible.</p></article><article><span>02</span><h3>Model transparency</h3><p>Every ticker model shows its status, training date, and historical test performance.</p></article><article><span>03</span><h3>Risk beside direction</h3><p>Confidence never stands alone. Signal disagreement, volatility, and data quality shape risk.</p></article><article><span>04</span><h3>Learn in context</h3><p>Understand RSI, MACD, trends, and volume while researching a real stock.</p></article></div>
      </section>
    </main>`,
})
export class HomeComponent {
  ticker = '';
  error = '';
  readonly popular = [
    { symbol: 'RELIANCE.NS', market: 'NSE' }, { symbol: 'TCS.NS', market: 'NSE' },
    { symbol: 'AAPL', market: 'NASDAQ' }, { symbol: 'MSFT', market: 'NASDAQ' },
    { symbol: 'NVDA', market: 'NASDAQ' }, { symbol: 'TSLA', market: 'NASDAQ' },
  ];
  constructor(private readonly router: Router) {}
  analyze(event: Event): void { event.preventDefault(); this.openTicker(); }
  openTicker(): void { this.error = tickerValidationMessage(this.ticker); if (!this.error) this.router.navigate(['/stocks', normalizeTicker(this.ticker)]); }
}
