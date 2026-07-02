import { Injectable, inject, signal } from '@angular/core';
import { PortfolioHolding } from './models';
import { StockApiService } from './stock-api.service';
import { normalizeTicker } from './ticker-validation';

const STORAGE_KEY = 'stock-analyzer-portfolio-v1';
const SYNC_DEBOUNCE_MS = 150;

export interface PortfolioHoldingDraft {
  ticker: string;
  quantity: number;
  averageCost: number;
  purchasedAt: string;
  note: string;
}

@Injectable({ providedIn: 'root' })
export class PortfolioService {
  private readonly api = inject(StockApiService);
  private localVersion = 0;
  private saveVersion = 0;
  private saveTimer: ReturnType<typeof setTimeout> | null = null;
  readonly holdings = signal<PortfolioHolding[]>(this.load());
  readonly syncState = signal<'local' | 'syncing' | 'synced' | 'offline'>('local');

  constructor() { this.hydrate(); }

  add(draft: PortfolioHoldingDraft): void {
    const holding: PortfolioHolding = {
      id: crypto.randomUUID(), ticker: normalizeTicker(draft.ticker), quantity: draft.quantity,
      average_cost: draft.averageCost, purchased_at: draft.purchasedAt || null, note: draft.note.trim().slice(0, 300),
    };
    this.set([...this.holdings(), holding].slice(0, 50));
  }

  remove(id: string): void { this.set(this.holdings().filter((holding) => holding.id !== id)); }

  updateNote(id: string, note: string): void {
    this.set(this.holdings().map((holding) => holding.id === id ? { ...holding, note: note.trim().slice(0, 300) } : holding));
  }

  private hydrate(): void {
    const hydrateVersion = this.localVersion;
    this.syncState.set('syncing');
    this.api.getWorkspacePortfolio().subscribe({
      next: (remote) => {
        if (this.localVersion !== hydrateVersion) {
          this.scheduleSync();
          return;
        }
        if (remote.length) { this.holdings.set(remote); this.persist(remote); this.syncState.set('synced'); }
        else if (this.holdings().length) this.scheduleSync();
        else this.syncState.set('synced');
      },
      error: () => this.syncState.set('offline'),
    });
  }

  private set(holdings: PortfolioHolding[]): void { this.localVersion++; this.holdings.set(holdings); this.persist(holdings); this.scheduleSync(); }
  private scheduleSync(): void {
    if (this.saveTimer) clearTimeout(this.saveTimer);
    this.saveTimer = setTimeout(() => {
      this.saveTimer = null;
      this.sync(this.holdings(), this.localVersion);
    }, SYNC_DEBOUNCE_MS);
  }
  private sync(holdings: PortfolioHolding[], version: number): void {
    const requestVersion = ++this.saveVersion;
    this.syncState.set('syncing');
    this.api.saveWorkspacePortfolio(holdings).subscribe({
      next: (saved) => {
        if (requestVersion === this.saveVersion && version === this.localVersion) {
          this.holdings.set(saved);
          this.persist(saved);
          this.syncState.set('synced');
        }
      },
      error: () => {
        if (requestVersion === this.saveVersion) this.syncState.set('offline');
      },
    });
  }
  private persist(holdings: PortfolioHolding[]): void { try { localStorage.setItem(STORAGE_KEY, JSON.stringify(holdings)); } catch { /* Offline cache is optional. */ } }
  private load(): PortfolioHolding[] { try { const value = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]'); return Array.isArray(value) ? value.slice(0, 50) : []; } catch { return []; } }
}
