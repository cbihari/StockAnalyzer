import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { AuthService } from './auth.service';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './signup.component.html',
  styleUrl: './auth.component.scss',
})
export class SignupComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  displayName = '';
  email = '';
  password = '';
  readonly loading = signal(false);
  readonly error = signal('');
  readonly googleUrl = this.auth.googleUrl;
  readonly googleEnabled = this.auth.googleEnabled;

  submit(): void {
    this.loading.set(true); this.error.set('');
    this.auth.signup(this.email, this.password, this.displayName).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (error) => this.error.set(this.auth.errorMessage(error, 'Account creation could not be completed.')),
    });
  }
}
