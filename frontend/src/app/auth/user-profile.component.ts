import { Component, ElementRef, HostListener, Input, Output, EventEmitter } from '@angular/core';
import { AuthUser } from '../core/models';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  templateUrl: './user-profile.component.html',
  styleUrl: './user-profile.component.scss',
})
export class UserProfileComponent {
  @Input({ required: true }) user!: AuthUser;
  @Output() readonly logoutRequested = new EventEmitter<void>();
  open = false;

  constructor(private readonly element: ElementRef<HTMLElement>) {}

  get initials(): string {
    const source = this.user.displayName?.trim() || this.user.email;
    return source.split(/\s+/).slice(0, 2).map((part) => part[0]).join('').toUpperCase();
  }

  toggle(): void {
    this.open = !this.open;
  }

  logout(): void {
    this.open = false;
    this.logoutRequested.emit();
  }

  @HostListener('document:click', ['$event'])
  closeOnOutsideClick(event: Event): void {
    if (!this.element.nativeElement.contains(event.target as Node)) this.open = false;
  }

  @HostListener('document:keydown.escape')
  closeOnEscape(): void {
    this.open = false;
  }
}
