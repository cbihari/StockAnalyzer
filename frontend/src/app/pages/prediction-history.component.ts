import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PredictionHistoryService } from '../core/prediction-history.service';

@Component({
  imports: [CommonModule, RouterLink],
  template: `<main class="page"><p class="eyebrow">PREDICTION HISTORY</p><h1>Your recent analyses</h1><p class="lead">Predictions viewed in this browser appear here.</p>
    @if (items.length) { <div class="table-card card"><table><thead><tr><th>Ticker</th><th>Signal</th><th>Confidence</th><th>UP probability</th><th>Checked</th></tr></thead><tbody>@for (item of items; track item.createdAt) { <tr><td><a [routerLink]="['/stocks', item.ticker]">{{ item.ticker }}</a></td><td><span class="badge" [class.down]="item.prediction === 'DOWN'">{{ item.prediction }}</span></td><td>{{ item.confidence }}%</td><td>{{ item.probability_up | percent:'1.0-1' }}</td><td>{{ item.createdAt | date:'medium' }}</td></tr> }</tbody></table></div> }
    @else { <div class="empty card"><h2>No predictions yet</h2><p>Analyze a ticker to start building your history.</p><a class="button" routerLink="/search">Search stocks</a></div> }
  </main>`,
})
export class PredictionHistoryComponent { readonly items = inject(PredictionHistoryService).getAll(); }
