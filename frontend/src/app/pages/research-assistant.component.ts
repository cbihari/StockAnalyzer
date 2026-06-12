import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AiResearchResponse } from '../core/models';
import { StockApiService } from '../core/stock-api.service';
import { normalizeTicker, tickerValidationMessage } from '../core/ticker-validation';
import { TickerAutocompleteComponent } from '../shared/ticker-autocomplete.component';

@Component({
  imports: [CommonModule, RouterLink, TickerAutocompleteComponent],
  template: `
    <main class="page assistant-page">
      <section class="assistant-hero">
        <div>
          <p class="eyebrow">AI RESEARCH ASSISTANT</p>
          <h1>Ask the evidence.<br><em>Keep the uncertainty.</em></h1>
          <p class="lead">Explore a stock's model output, technical signals, market context, and headline sentiment through grounded answers.</p>
        </div>
        <aside class="assistant-guardrail card">
          <span>RESEARCH MODE</span>
          <strong>No trade recommendations</strong>
          <p>Answers are restricted to StockAnalyzer's latest timestamped snapshot. Missing or conflicting evidence stays visible.</p>
        </aside>
      </section>

      <section class="assistant-composer card" aria-labelledby="assistant-question-heading">
        <div class="assistant-fields">
          <label>
            <span>STOCK</span>
            <app-ticker-autocomplete inputId="assistant-ticker" ariaLabel="Stock ticker" placeholder="AAPL or RELIANCE.NS" [value]="ticker()" (valueChange)="ticker.set($event)" (submitted)="ask()" />
          </label>
          <label class="question-field">
            <span id="assistant-question-heading">YOUR RESEARCH QUESTION</span>
            <textarea maxlength="300" rows="3" [value]="question()" placeholder="Why does the model lean UP, and what evidence conflicts with it?" (input)="question.set($any($event.target).value)" (keydown.control.enter)="ask()"></textarea>
            <small>{{ question().length }}/300 · Ctrl + Enter to ask</small>
          </label>
        </div>
        <div class="assistant-suggestions" aria-label="Suggested questions">
          @for (suggestion of suggestions; track suggestion) {
            <button type="button" (click)="useSuggestion(suggestion)">{{ suggestion }}</button>
          }
        </div>
        @if (error()) { <p class="form-error" role="alert">{{ error() }}</p> }
        <div class="assistant-actions">
          <p>First-time analysis may train a ticker model and take longer.</p>
          <button type="button" class="primary-action" [disabled]="loading()" (click)="ask()">
            @if (loading()) { <span class="spinner" aria-hidden="true"></span> Reading the evidence... } @else { Ask StockAnalyzer → }
          </button>
        </div>
      </section>

      @if (!result() && !loading()) {
        <section class="assistant-empty">
          <div class="assistant-orbit"><span>✦</span></div>
          <h2>A research answer should show its work.</h2>
          <p>Choose a ticker and ask about prediction drivers, risk, model quality, indicators, recent range, or headline sentiment.</p>
        </section>
      }

      @if (loading()) {
        <section class="assistant-loading card" role="status">
          <div class="skeleton wide"></div><div class="skeleton"></div><div class="skeleton short"></div>
          <p>Collecting the latest prediction, indicators, market context, and news evidence...</p>
        </section>
      }

      @if (result(); as response) {
        <section class="assistant-result" aria-live="polite">
          <article class="assistant-answer card">
            <div class="answer-meta">
              <div><span>{{ response.ticker }}</span><small>{{ response.generatedAt | date:'medium' }}</small></div>
              <div class="provider-badges"><b [class.fallback]="response.fallbackUsed">{{ response.provider === 'openai' ? 'OPENAI GROUNDED' : 'DETERMINISTIC' }}</b>@if (response.cached) { <b>CACHED</b> }</div>
            </div>
            <p class="asked-question">“{{ response.answer.question }}”</p>
            <h2>{{ response.answer.answer }}</h2>
            @if (response.answer.key_points.length) {
              <div class="answer-points">@for (point of response.answer.key_points; track point) { <p><span>✓</span>{{ point }}</p> }</div>
            }
            @if (response.fallbackUsed) { <div class="fallback-note">OpenAI was unavailable, so StockAnalyzer used its deterministic research engine. The evidence remains current and traceable.</div> }
            <div class="answer-footer"><span>{{ response.model }}</span><a [routerLink]="['/stocks', response.ticker]">Open full analysis →</a></div>
          </article>

          <aside class="assistant-evidence">
            <div class="evidence-heading"><p class="eyebrow">EVIDENCE LEDGER</p><h2>Sources used</h2></div>
            @for (citation of response.answer.citations; track citation.source + citation.label) {
              <article class="evidence-card card">
                <div><span>{{ sourceLabel(citation.source) }}</span><time>{{ citation.observed_at | date:'MMM d, HH:mm' }}</time></div>
                <strong>{{ citation.label }}</strong><p>{{ citation.evidence }}</p>
              </article>
            }
          </aside>
        </section>

        @if (response.answer.limitations.length) {
          <section class="assistant-limitations card"><span>DATA LIMITATIONS</span>@for (item of response.answer.limitations; track item) { <p>{{ item }}</p> }</section>
        }
        <section class="assistant-followups">
          <div><p class="eyebrow">KEEP EXPLORING</p><h2>Useful follow-ups</h2></div>
          <div>@for (item of response.answer.follow_up_questions; track item) { <button type="button" (click)="useSuggestion(item); ask()">{{ item }} →</button> }</div>
        </section>
        <p class="assistant-disclaimer">{{ response.answer.disclaimer }} AI output may contain errors; review the cited analysis data.</p>
      }
    </main>`,
})
export class ResearchAssistantComponent {
  private readonly api = inject(StockApiService);
  readonly ticker = signal('AAPL');
  readonly question = signal('Why does the model lean in this direction, and what evidence conflicts with it?');
  readonly result = signal<AiResearchResponse | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly suggestions = [
    'Which signals support the prediction?',
    'What are the main risks?',
    'Explain RSI in this analysis',
    'Does recent news support the model?',
    'Confidence versus model accuracy?',
  ];

  useSuggestion(question: string): void { this.question.set(question); }

  ask(): void {
    if (this.loading()) return;
    const ticker = normalizeTicker(this.ticker());
    const tickerError = tickerValidationMessage(ticker);
    const question = this.question().trim().replace(/\s+/g, ' ');
    if (tickerError) { this.error.set(tickerError); return; }
    if (question.length < 3) { this.error.set('Enter a research question with at least 3 characters.'); return; }
    this.ticker.set(ticker); this.question.set(question); this.error.set(''); this.loading.set(true);
    this.api.askAiResearch(ticker, question).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (result) => this.result.set(result),
      error: (error: HttpErrorResponse) => this.error.set(error.error?.detail || 'The research assistant is unavailable right now.'),
    });
  }

  sourceLabel(source: string): string {
    return source.replaceAll('_', ' ').toUpperCase();
  }
}
