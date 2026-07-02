import { Component, effect, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './auth/auth.service';
import { UserProfileComponent } from './auth/user-profile.component';
import { AlertService } from './core/alert.service';
import { ClientIdentityService } from './core/client-identity.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, UserProfileComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  readonly claimPrompt = signal(false);
  readonly claimLoading = signal(false);

  constructor(
    readonly alerts: AlertService,
    readonly auth: AuthService,
    private readonly identity: ClientIdentityService,
    private readonly router: Router,
  ) {
    effect(() => {
      const user = this.auth.currentUser();
      if (!user) { this.claimPrompt.set(false); return; }
      this.claimPrompt.set(!this.claimHandled(user.id));
    });
  }

  claimWorkspace(): void {
    const user = this.auth.currentUser();
    if (!user) return;
    this.claimLoading.set(true);
    this.auth.claimWorkspace().subscribe({
      next: () => { this.markClaimHandled(user.id); this.claimPrompt.set(false); this.claimLoading.set(false); },
      error: () => this.claimLoading.set(false),
    });
  }

  skipClaim(): void {
    const user = this.auth.currentUser();
    if (user) this.markClaimHandled(user.id);
    this.claimPrompt.set(false);
  }

  logout(): void {
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }

  private claimHandled(userId: string): boolean {
    try { return localStorage.getItem(this.claimKey(userId)) === this.identity.id; } catch { return false; }
  }

  private markClaimHandled(userId: string): void {
    try { localStorage.setItem(this.claimKey(userId), this.identity.id); } catch { /* Prompt may return next session. */ }
  }

  private claimKey(userId: string): string { return `stock-analyzer-workspace-claim-${userId}`; }
}
