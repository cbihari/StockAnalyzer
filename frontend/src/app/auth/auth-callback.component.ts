import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  standalone: true,
  template: '<main class="page"><div class="loading card" role="status"><span class="spinner"></span> Completing sign in...</div></main>',
})
export class AuthCallbackComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  ngOnInit(): void {
    const token = new URLSearchParams(this.route.snapshot.fragment ?? '').get('token');
    if (token) this.auth.acceptToken(token);
    this.router.navigateByUrl(token ? '/' : '/login');
  }
}
