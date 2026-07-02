import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, of, tap, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthConfig, AuthResponse, AuthUser } from '../core/models';
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
  readonly googleEnabled = signal(false);
  readonly googleUrl = `${environment.authUrl}/api/auth/google`;

  constructor() {
    this.loadConfig();
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

  acceptToken(token: string): Observable<AuthUser> {
    this.storeToken(token);
    return this.getCurrentUser().pipe(
      tap((user) => this.currentUser.set(user)),
      catchError((error) => {
        this.logout();
        return throwError(() => error);
      }),
    );
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

  errorMessage(error: { status?: number; error?: { detail?: string } }, fallback: string): string {
    if (error.status === 0) return 'Authentication service is temporarily unreachable. Please try again.';
    if (error.status === 404) return 'Authentication is not available on the deployed API yet.';
    return error.error?.detail ?? fallback;
  }

  private loadConfig(): void {
    this.http.get<AuthConfig>(`${this.baseUrl}/api/auth/config`).pipe(
      catchError(() => of({ googleEnabled: false })),
    ).subscribe((config) => this.googleEnabled.set(config.googleEnabled));
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
