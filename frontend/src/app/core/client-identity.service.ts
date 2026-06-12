import { Injectable } from '@angular/core';

const STORAGE_KEY = 'stock-analyzer-client-id-v1';

@Injectable({ providedIn: 'root' })
export class ClientIdentityService {
  readonly id = this.load();

  private load(): string {
    try {
      const existing = localStorage.getItem(STORAGE_KEY);
      if (existing) return existing;
      const created = crypto.randomUUID();
      localStorage.setItem(STORAGE_KEY, created);
      return created;
    } catch { return crypto.randomUUID(); }
  }
}
