import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthResponse, AuthUser } from '../core/models';
import { ClientIdentityService } from '../core/client-identity.service';
import { AuthTokenStore } from './auth-token.store';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly identity = inject(ClientIdentityService);
  private readonly tokenStore = inject(AuthTokenStore);
  private readonly baseUrl = environment.apiUrl;
  readonly token = this.tokenStore.token;
  readonly currentUser = signal<AuthUser | null>(null);
  readonly authenticated = computed(() => !!this.currentUser());
  readonly googleUrl = `${this.baseUrl}/api/auth/google`;

  constructor() {
    if (this.token()) this.refreshCurrentUser();
  }

  signup(email: string, password: string, displayName: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/api/auth/signup`, { email, password, displayName })
      .pipe(tap((response) => this.setSession(response)));
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/api/auth/login`, { email, password })
      .pipe(tap((response) => this.setSession(response)));
  }

  acceptToken(token: string): void {
    this.storeToken(token);
    this.refreshCurrentUser();
  }

  getCurrentUser(): Observable<AuthUser> {
    return this.http.get<AuthUser>(`${this.baseUrl}/api/auth/me`);
  }

  claimWorkspace(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/api/account/claim-workspace`, {
      workspaceId: this.identity.id,
    });
  }

  logout(): void {
    this.tokenStore.clear();
    this.currentUser.set(null);
  }

  private refreshCurrentUser(): void {
    this.getCurrentUser().subscribe({
      next: (user) => this.currentUser.set(user),
      error: () => this.logout(),
    });
  }

  private setSession(response: AuthResponse): void {
    this.storeToken(response.token);
    this.currentUser.set(response.user);
  }

  private storeToken(token: string): void {
    this.tokenStore.set(token);
  }
}
