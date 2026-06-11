import { Injectable } from '@angular/core';
import { MlPrediction, PredictionHistoryItem } from './models';

@Injectable({ providedIn: 'root' })
export class PredictionHistoryService {
  private readonly key = 'stock-analyzer-prediction-history';

  getAll(): PredictionHistoryItem[] {
    try {
      return JSON.parse(localStorage.getItem(this.key) ?? '[]') as PredictionHistoryItem[];
    } catch {
      return [];
    }
  }

  add(prediction: MlPrediction): void {
    const existing = this.getAll();
    const item: PredictionHistoryItem = { ...prediction, createdAt: new Date().toISOString() };
    localStorage.setItem(this.key, JSON.stringify([item, ...existing].slice(0, 30)));
  }
}
