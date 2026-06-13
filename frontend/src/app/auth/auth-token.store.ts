import { Injectable, signal } from '@angular/core';

const TOKEN_KEY = 'stock-analyzer-auth-token-v1';

@Injectable({ providedIn: 'root' })
export class AuthTokenStore {
  readonly token = signal(this.load());

  set(token: string): void {
    this.token.set(token);
    try { sessionStorage.setItem(TOKEN_KEY, token); } catch { /* Keep the token in memory. */ }
  }

  clear(): void {
    this.token.set('');
    try { sessionStorage.removeItem(TOKEN_KEY); } catch { /* Session storage is optional. */ }
  }

  private load(): string {
    try { return sessionStorage.getItem(TOKEN_KEY) ?? ''; } catch { return ''; }
  }
}
