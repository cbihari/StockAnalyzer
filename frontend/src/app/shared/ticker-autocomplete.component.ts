import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges, inject, signal } from '@angular/core';
import { Subject, catchError, debounceTime, distinctUntilChanged, finalize, of, switchMap, takeUntil } from 'rxjs';
import { StockSuggestion } from '../core/models';
import { StockApiService } from '../core/stock-api.service';
import { normalizeTicker, tickerValidationMessage } from '../core/ticker-validation';

@Component({
  selector: 'app-ticker-autocomplete',
  imports: [CommonModule],
  template: `
    <div class="ticker-autocomplete">
      <input
        [id]="inputId"
        [attr.aria-label]="ariaLabel"
        [attr.aria-describedby]="describedBy || null"
        [attr.aria-expanded]="open()"
        [attr.aria-activedescendant]="activeIndex() >= 0 ? optionId(activeIndex()) : null"
        aria-autocomplete="list"
        aria-controls="ticker-suggestions"
        role="combobox"
        [placeholder]="placeholder"
        [value]="value"
        maxlength="20"
        autocomplete="off"
        (input)="onInput($any($event.target).value)"
        (focus)="onFocus()"
        (blur)="onBlur()"
        (keydown)="onKeydown($event)"
      />
      @if (open()) {
        <div id="ticker-suggestions" class="autocomplete-dropdown" role="listbox" aria-label="Stock suggestions">
          @if (searching()) {
            <div class="autocomplete-state" role="status"><span class="spinner" aria-hidden="true"></span> Searching stocks...</div>
          } @else if (suggestions().length === 0) {
            <div class="autocomplete-state">No stocks found</div>
          } @else {
            @for (stock of suggestions(); track stock.symbol; let index = $index) {
              <button
                type="button"
                class="autocomplete-option"
                [class.active]="activeIndex() === index"
                [id]="optionId(index)"
                role="option"
                [attr.aria-selected]="activeIndex() === index"
                (mousedown)="select(stock, $event)"
              >
                <span class="suggestion-main"><strong>{{ stock.symbol }}</strong><span>{{ stock.name }}</span><small>{{ stock.country }} · {{ stock.type }}</small></span>
                <span class="exchange-badge">{{ stock.exchange }}</span>
              </button>
            }
          }
        </div>
      }
    </div>`,
})
export class TickerAutocompleteComponent implements OnChanges, OnDestroy {
  private readonly api = inject(StockApiService);
  private readonly queryChanges = new Subject<string>();
  private readonly destroyed = new Subject<void>();
  private blurTimer: ReturnType<typeof setTimeout> | undefined;

  @Input() value = '';
  @Input() inputId = 'ticker';
  @Input() ariaLabel = 'Ticker symbol';
  @Input() describedBy = '';
  @Input() placeholder = 'e.g. RELIANCE.NS or AAPL';
  @Output() readonly valueChange = new EventEmitter<string>();
  @Output() readonly tickerSelected = new EventEmitter<string>();
  @Output() readonly submitted = new EventEmitter<void>();

  readonly suggestions = signal<StockSuggestion[]>([]);
  readonly searching = signal(false);
  readonly open = signal(false);
  readonly activeIndex = signal(-1);

  constructor() {
    this.queryChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap((query) => {
        if (query.trim().length < 2) {
          this.searching.set(false); this.suggestions.set([]); this.open.set(false);
          return of<StockSuggestion[]>([]);
        }
        this.searching.set(true); this.open.set(true);
        return this.api.searchStocks(query).pipe(
          catchError(() => of<StockSuggestion[]>([])),
          finalize(() => this.searching.set(false)),
        );
      }),
      takeUntil(this.destroyed),
    ).subscribe((suggestions) => {
      this.suggestions.set(suggestions); this.activeIndex.set(suggestions.length ? 0 : -1);
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['value'] && this.value.trim().length < 2) {
      this.open.set(false); this.suggestions.set([]); this.activeIndex.set(-1);
    }
  }

  ngOnDestroy(): void { this.destroyed.next(); this.destroyed.complete(); this.clearBlurTimer(); }

  onInput(value: string): void {
    this.value = value; this.valueChange.emit(value); this.activeIndex.set(-1);
    this.queryChanges.next(value);
  }

  onFocus(): void {
    this.clearBlurTimer();
    if (this.searching() || this.suggestions().length > 0) this.open.set(true);
  }

  onBlur(): void { this.blurTimer = setTimeout(() => this.open.set(false), 120); }

  onKeydown(event: KeyboardEvent): void {
    const count = this.suggestions().length;
    if (event.key === 'ArrowDown' && this.open() && count) {
      event.preventDefault(); this.activeIndex.update((index) => (index + 1) % count); return;
    }
    if (event.key === 'ArrowUp' && this.open() && count) {
      event.preventDefault(); this.activeIndex.update((index) => (index <= 0 ? count - 1 : index - 1)); return;
    }
    if (event.key === 'Escape') {
      event.preventDefault(); this.open.set(false); this.activeIndex.set(-1); return;
    }
    if (event.key === 'Enter') {
      event.preventDefault();
      const selected = this.suggestions()[this.activeIndex()];
      if (this.open() && selected) this.choose(selected);
      this.submitted.emit();
    }
  }

  select(stock: StockSuggestion, event: MouseEvent): void { event.preventDefault(); this.choose(stock); }
  optionId(index: number): string { return `${this.inputId}-suggestion-${index}`; }

  private choose(stock: StockSuggestion): void {
    const ticker = normalizeTicker(stock.symbol);
    if (tickerValidationMessage(ticker)) return;
    this.value = ticker; this.valueChange.emit(ticker); this.tickerSelected.emit(ticker);
    this.open.set(false); this.activeIndex.set(-1);
  }

  private clearBlurTimer(): void { if (this.blurTimer !== undefined) { clearTimeout(this.blurTimer); this.blurTimer = undefined; } }
}
