import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'stock-analyzer-learning-progress-v1';

@Injectable({ providedIn: 'root' })
export class LearningProgressService {
  readonly completed = signal<string[]>(this.load());

  has(slug: string): boolean { return this.completed().includes(slug); }

  markComplete(slug: string): void {
    if (this.has(slug)) return;
    const next = [...this.completed(), slug];
    this.completed.set(next);
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(next)); } catch { /* Optional guest persistence. */ }
  }

  private load(): string[] {
    try {
      const value = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]');
      return Array.isArray(value) ? value.filter((item): item is string => typeof item === 'string') : [];
    } catch { return []; }
  }
}
