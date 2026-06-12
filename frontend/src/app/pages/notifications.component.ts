import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AlertService } from '../core/alert.service';

@Component({
  imports: [CommonModule, RouterLink],
  template: `
    <main class="page notifications-page">
      <div class="notifications-heading"><div><p class="eyebrow">NOTIFICATION CENTER</p><h1>Research alerts</h1><p class="lead">Triggered conditions from delayed watchlist data, with timestamps and direct links to supporting evidence.</p></div>@if (alerts.notifications().length) { <div><button type="button" class="secondary-button" (click)="alerts.markAllRead()">Mark all read</button><button type="button" class="secondary-button" (click)="alerts.clear()">Clear</button></div> }</div>
      <div class="notice warning">Alerts are evaluated only while this browser refreshes the watchlist. They are educational monitoring tools, not real-time trading signals. <strong>{{ alerts.syncState() === 'synced' ? 'Notification state synced.' : alerts.syncState() === 'offline' ? 'Offline cache active.' : 'Syncing...' }}</strong></div>
      @if (!alerts.notifications().length) { <div class="empty card"><h2>No alerts have triggered</h2><p>Create a price or daily-move alert from your watchlist.</p><a class="link-button" routerLink="/watchlist">Open watchlist</a></div> }
      @if (alerts.notifications().length) { <section class="notification-list">@for (item of alerts.notifications(); track item.id) { <article class="card notification-card" [class.unread]="!item.read"><div class="notification-status"><span></span><small>{{ item.read ? 'READ' : 'NEW' }}</small></div><div><div class="notification-title"><strong>{{ item.title }}</strong><time>{{ item.triggeredAt | date:'medium' }}</time></div><p>{{ item.message }}</p><small>Market data timestamp: {{ item.dataTimestamp | date:'medium' }}</small><a [routerLink]="item.evidenceUrl" (click)="alerts.markRead(item.id)">Review evidence →</a></div><button type="button" class="secondary-button" [disabled]="item.read" (click)="alerts.markRead(item.id)">{{ item.read ? 'Read' : 'Mark read' }}</button></article> }</section> }
    </main>`,
})
export class NotificationsComponent { readonly alerts = inject(AlertService); }
