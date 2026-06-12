import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/home.component').then((m) => m.HomeComponent) },
  { path: 'search', loadComponent: () => import('./pages/stock-search.component').then((m) => m.StockSearchComponent) },
  { path: 'market', loadComponent: () => import('./pages/market-overview.component').then((m) => m.MarketOverviewComponent) },
  { path: 'stocks/:ticker', loadComponent: () => import('./pages/stock-detail.component').then((m) => m.StockDetailComponent) },
  { path: 'compare', loadComponent: () => import('./pages/stock-comparison.component').then((m) => m.StockComparisonComponent) },
  { path: 'watchlist', loadComponent: () => import('./pages/watchlist.component').then((m) => m.WatchlistComponent) },
  { path: 'learn', loadComponent: () => import('./pages/learning-center.component').then((m) => m.LearningCenterComponent) },
  { path: 'learn/:slug', loadComponent: () => import('./pages/learning-center.component').then((m) => m.LearningCenterComponent) },
  { path: 'notifications', loadComponent: () => import('./pages/notifications.component').then((m) => m.NotificationsComponent) },
  { path: 'history', loadComponent: () => import('./pages/prediction-history.component').then((m) => m.PredictionHistoryComponent) },
  { path: 'accuracy', loadComponent: () => import('./pages/model-accuracy.component').then((m) => m.ModelAccuracyComponent) },
  { path: '**', redirectTo: '' },
];
