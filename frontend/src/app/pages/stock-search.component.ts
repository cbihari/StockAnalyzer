import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { normalizeTicker, tickerValidationMessage } from '../core/ticker-validation';

@Component({
  imports: [FormsModule],
  template: `
    <main class="page narrow-page">
      <p class="eyebrow">STOCK SEARCH</p><h1>Which stock should we analyze?</h1>
      <p class="lead">Use the full Yahoo Finance ticker. Indian NSE symbols usually end in <strong>.NS</strong>.</p>
      <form class="search-panel card" (ngSubmit)="search()" novalidate>
        <label for="ticker">Ticker symbol</label>
        <div class="search-row"><input id="ticker" name="ticker" [(ngModel)]="ticker" (ngModelChange)="validationError = ''" placeholder="RELIANCE.NS" maxlength="20" autocomplete="off" aria-describedby="ticker-help ticker-error" required /><button type="submit">Open dashboard</button></div>
        <p id="ticker-help" class="field-help">Example: RELIANCE.NS. A model is loaded or trained automatically for each valid ticker.</p>
        <div class="notice warning compact-warning">First-time analysis may take longer because the model is trained for this ticker.</div>
        @if (validationError) { <p id="ticker-error" class="field-error" role="alert">{{ validationError }}</p> }
        <div class="examples"><span>Trained example:</span><button type="button" (click)="open('RELIANCE.NS')">RELIANCE.NS</button></div>
      </form>
    </main>`,
})
export class StockSearchComponent {
  ticker = 'RELIANCE.NS';
  validationError = '';
  constructor(private readonly router: Router) {}
  search(): void { this.open(this.ticker); }
  open(ticker: string): void {
    this.validationError = tickerValidationMessage(ticker);
    if (this.validationError) return;
    this.router.navigate(['/stocks', normalizeTicker(ticker)]);
  }
}
