import { Injectable, inject, signal } from '@angular/core';
import { PortfolioHolding } from './models';
import { StockApiService } from './stock-api.service';
import { normalizeTicker } from './ticker-validation';

const STORAGE_KEY = 'stock-analyzer-portfolio-v1';

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
    this.syncState.set('syncing');
    this.api.getWorkspacePortfolio().subscribe({
      next: (remote) => {
        if (remote.length) { this.holdings.set(remote); this.persist(remote); this.syncState.set('synced'); }
        else if (this.holdings().length) this.sync(this.holdings());
        else this.syncState.set('synced');
      },
      error: () => this.syncState.set('offline'),
    });
  }

  private set(holdings: PortfolioHolding[]): void { this.holdings.set(holdings); this.persist(holdings); this.sync(holdings); }
  private sync(holdings: PortfolioHolding[]): void { this.syncState.set('syncing'); this.api.saveWorkspacePortfolio(holdings).subscribe({ next: (saved) => { this.holdings.set(saved); this.persist(saved); this.syncState.set('synced'); }, error: () => this.syncState.set('offline') }); }
  private persist(holdings: PortfolioHolding[]): void { try { localStorage.setItem(STORAGE_KEY, JSON.stringify(holdings)); } catch { /* Offline cache is optional. */ } }
  private load(): PortfolioHolding[] { try { const value = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]'); return Array.isArray(value) ? value.slice(0, 50) : []; } catch { return []; } }
}
