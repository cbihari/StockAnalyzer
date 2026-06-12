import { Injectable, computed, signal } from '@angular/core';
import { normalizeTicker } from './ticker-validation';

const STORAGE_KEY = 'stock-analyzer-watchlist-v2';
const LEGACY_STORAGE_KEY = 'stock-analyzer-watchlist-v1';
const DEFAULT_TICKERS = ['RELIANCE.NS', 'AAPL', 'MSFT'];

export interface WatchlistItem {
  ticker: string;
  addedAt: string;
  note: string;
  tags: string[];
}

@Injectable({ providedIn: 'root' })
export class WatchlistService {
  readonly items = signal<WatchlistItem[]>(this.load());
  readonly tickers = computed(() => this.items().map((item) => item.ticker));

  has(ticker: string): boolean { return this.tickers().includes(normalizeTicker(ticker)); }
  get(ticker: string): WatchlistItem | undefined { return this.items().find((item) => item.ticker === normalizeTicker(ticker)); }

  toggle(ticker: string): boolean {
    const normalized = normalizeTicker(ticker);
    const saved = !this.has(normalized);
    this.set(saved
      ? [...this.items(), this.createItem(normalized)].slice(0, 10)
      : this.items().filter((item) => item.ticker !== normalized));
    return saved;
  }

  updateDetails(ticker: string, note: string, tags: string[]): void {
    const normalized = normalizeTicker(ticker);
    const cleanTags = [...new Set(tags.map((tag) => tag.trim().toLowerCase()).filter(Boolean))].slice(0, 5);
    this.set(this.items().map((item) => item.ticker === normalized
      ? { ...item, note: note.trim().slice(0, 500), tags: cleanTags }
      : item));
  }

  remove(ticker: string): void {
    const normalized = normalizeTicker(ticker);
    this.set(this.items().filter((item) => item.ticker !== normalized));
  }

  private createItem(ticker: string): WatchlistItem {
    return { ticker, addedAt: new Date().toISOString(), note: '', tags: [] };
  }

  private set(items: WatchlistItem[]): void {
    this.items.set(items);
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(items)); } catch { /* Storage can be unavailable in private contexts. */ }
  }

  private load(): WatchlistItem[] {
    try {
      const saved = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? 'null');
      if (Array.isArray(saved)) {
        return saved.filter(this.isItem).slice(0, 10).map((item) => ({
          ticker: normalizeTicker(item.ticker),
          addedAt: item.addedAt,
          note: item.note.slice(0, 500),
          tags: item.tags.slice(0, 5),
        }));
      }
      const legacy = JSON.parse(localStorage.getItem(LEGACY_STORAGE_KEY) ?? 'null');
      if (Array.isArray(legacy)) {
        const migrated = legacy.filter((item): item is string => typeof item === 'string').slice(0, 10).map((ticker) => this.createItem(normalizeTicker(ticker)));
        this.persistMigration(migrated);
        return migrated;
      }
    } catch { /* Fall back to starter research symbols. */ }
    return DEFAULT_TICKERS.map((ticker) => this.createItem(ticker));
  }

  private isItem(value: unknown): value is WatchlistItem {
    if (!value || typeof value !== 'object') return false;
    const item = value as Partial<WatchlistItem>;
    return typeof item.ticker === 'string' && typeof item.addedAt === 'string' && typeof item.note === 'string' && Array.isArray(item.tags);
  }

  private persistMigration(items: WatchlistItem[]): void {
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(items)); } catch { /* Migration remains in memory. */ }
  }
}
