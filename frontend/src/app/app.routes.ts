import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/home.component').then((m) => m.HomeComponent) },
  { path: 'search', loadComponent: () => import('./pages/stock-search.component').then((m) => m.StockSearchComponent) },
  { path: 'stocks/:ticker', loadComponent: () => import('./pages/stock-detail.component').then((m) => m.StockDetailComponent) },
  { path: 'history', loadComponent: () => import('./pages/prediction-history.component').then((m) => m.PredictionHistoryComponent) },
  { path: 'accuracy', loadComponent: () => import('./pages/model-accuracy.component').then((m) => m.ModelAccuracyComponent) },
  { path: '**', redirectTo: '' },
];
