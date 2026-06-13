import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { AuthService } from './auth.service';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './auth.component.scss',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  email = '';
  password = '';
  readonly loading = signal(false);
  readonly error = signal('');
  readonly googleUrl = this.auth.googleUrl;

  submit(): void {
    this.loading.set(true); this.error.set('');
    this.auth.login(this.email, this.password).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (error) => this.error.set(error.error?.detail ?? 'Login could not be completed.'),
    });
  }
}
