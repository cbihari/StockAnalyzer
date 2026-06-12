import { Injectable, computed, signal } from '@angular/core';
import { MarketInstrument } from './models';
import { normalizeTicker } from './ticker-validation';

const RULES_KEY = 'stock-analyzer-alert-rules-v1';
const NOTIFICATIONS_KEY = 'stock-analyzer-notifications-v1';

export type AlertType = 'price_above' | 'price_below' | 'daily_move';
export type AlertFrequency = 'once' | 'daily';

export interface AlertRule {
  id: string;
  ticker: string;
  type: AlertType;
  threshold: number;
  frequency: AlertFrequency;
  cooldownHours: number;
  quietStart: string;
  quietEnd: string;
  enabled: boolean;
  createdAt: string;
  lastTriggeredAt: string | null;
}

export interface AlertNotification {
  id: string;
  alertId: string;
  ticker: string;
  title: string;
  message: string;
  triggeredAt: string;
  dataTimestamp: string;
  evidenceUrl: string;
  read: boolean;
}

export interface AlertDraft {
  type: AlertType;
  threshold: number;
  frequency: AlertFrequency;
  cooldownHours: number;
  quietStart: string;
  quietEnd: string;
}

@Injectable({ providedIn: 'root' })
export class AlertService {
  readonly rules = signal<AlertRule[]>(this.load<AlertRule[]>(RULES_KEY, []));
  readonly notifications = signal<AlertNotification[]>(this.load<AlertNotification[]>(NOTIFICATIONS_KEY, []));
  readonly unreadCount = computed(() => this.notifications().filter((item) => !item.read).length);

  rulesFor(ticker: string): AlertRule[] {
    const normalized = normalizeTicker(ticker);
    return this.rules().filter((rule) => rule.ticker === normalized);
  }

  add(ticker: string, draft: AlertDraft): AlertRule {
    const rule: AlertRule = {
      id: crypto.randomUUID(), ticker: normalizeTicker(ticker), type: draft.type,
      threshold: Math.abs(Number(draft.threshold)), frequency: draft.frequency,
      cooldownHours: Math.max(1, Math.min(168, Number(draft.cooldownHours) || 24)),
      quietStart: draft.quietStart, quietEnd: draft.quietEnd, enabled: true,
      createdAt: new Date().toISOString(), lastTriggeredAt: null,
    };
    this.setRules([...this.rules(), rule]);
    return rule;
  }

  remove(id: string): void { this.setRules(this.rules().filter((rule) => rule.id !== id)); }
  toggle(id: string): void { this.setRules(this.rules().map((rule) => rule.id === id ? { ...rule, enabled: !rule.enabled } : rule)); }

  evaluate(quotes: MarketInstrument[], dataTimestamp: string, now = new Date()): AlertNotification[] {
    const quoteMap = new Map(quotes.map((quote) => [quote.symbol, quote]));
    const triggered: AlertNotification[] = [];
    const updatedRules = this.rules().map((rule) => {
      const quote = quoteMap.get(rule.ticker);
      if (!quote || !rule.enabled || this.isQuiet(rule, now) || !this.canTrigger(rule, now) || !this.matches(rule, quote)) return rule;
      const notification = this.createNotification(rule, quote, dataTimestamp, now);
      triggered.push(notification);
      return { ...rule, enabled: rule.frequency !== 'once', lastTriggeredAt: now.toISOString() };
    });
    if (triggered.length) {
      this.setRules(updatedRules);
      this.setNotifications([...triggered, ...this.notifications()].slice(0, 100));
    }
    return triggered;
  }

  markRead(id: string): void { this.setNotifications(this.notifications().map((item) => item.id === id ? { ...item, read: true } : item)); }
  markAllRead(): void { this.setNotifications(this.notifications().map((item) => ({ ...item, read: true }))); }
  clear(): void { this.setNotifications([]); }

  describe(rule: AlertRule): string {
    if (rule.type === 'price_above') return `Price above ${rule.threshold.toFixed(2)}`;
    if (rule.type === 'price_below') return `Price below ${rule.threshold.toFixed(2)}`;
    return `Daily move reaches ${rule.threshold.toFixed(1)}%`;
  }

  private matches(rule: AlertRule, quote: MarketInstrument): boolean {
    if (rule.type === 'price_above') return quote.price >= rule.threshold;
    if (rule.type === 'price_below') return quote.price <= rule.threshold;
    return Math.abs(quote.change_percent * 100) >= rule.threshold;
  }

  private canTrigger(rule: AlertRule, now: Date): boolean {
    if (!rule.lastTriggeredAt) return true;
    return now.getTime() - new Date(rule.lastTriggeredAt).getTime() >= rule.cooldownHours * 3_600_000;
  }

  private isQuiet(rule: AlertRule, now: Date): boolean {
    if (!rule.quietStart || !rule.quietEnd || rule.quietStart === rule.quietEnd) return false;
    const current = now.getHours() * 60 + now.getMinutes();
    const start = this.minutes(rule.quietStart); const end = this.minutes(rule.quietEnd);
    return start < end ? current >= start && current < end : current >= start || current < end;
  }

  private minutes(value: string): number { const [hour, minute] = value.split(':').map(Number); return hour * 60 + minute; }

  private createNotification(rule: AlertRule, quote: MarketInstrument, dataTimestamp: string, now: Date): AlertNotification {
    const direction = quote.change_percent >= 0 ? 'up' : 'down';
    return {
      id: crypto.randomUUID(), alertId: rule.id, ticker: rule.ticker,
      title: `${rule.ticker} alert triggered`,
      message: `${this.describe(rule)}. Latest delayed price ${quote.price.toFixed(2)}, ${direction} ${Math.abs(quote.change_percent * 100).toFixed(2)}% for the session.`,
      triggeredAt: now.toISOString(), dataTimestamp, evidenceUrl: `/stocks/${encodeURIComponent(rule.ticker)}`, read: false,
    };
  }

  private setRules(rules: AlertRule[]): void { this.rules.set(rules); this.save(RULES_KEY, rules); }
  private setNotifications(items: AlertNotification[]): void { this.notifications.set(items); this.save(NOTIFICATIONS_KEY, items); }
  private save(key: string, value: unknown): void { try { localStorage.setItem(key, JSON.stringify(value)); } catch { /* Optional guest persistence. */ } }
  private load<T>(key: string, fallback: T): T { try { return JSON.parse(localStorage.getItem(key) ?? 'null') ?? fallback; } catch { return fallback; } }
}
