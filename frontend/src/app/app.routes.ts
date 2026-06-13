import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/home.component').then((m) => m.HomeComponent) },
  { path: 'search', loadComponent: () => import('./pages/stock-search.component').then((m) => m.StockSearchComponent) },
  { path: 'market', loadComponent: () => import('./pages/market-overview.component').then((m) => m.MarketOverviewComponent) },
  { path: 'stocks/:ticker/news', loadComponent: () => import('./pages/stock-news.component').then((m) => m.StockNewsComponent) },
  { path: 'stocks/:ticker', loadComponent: () => import('./pages/stock-detail.component').then((m) => m.StockDetailComponent) },
  { path: 'compare', loadComponent: () => import('./pages/stock-comparison.component').then((m) => m.StockComparisonComponent) },
  { path: 'assistant', loadComponent: () => import('./pages/research-assistant.component').then((m) => m.ResearchAssistantComponent) },
  { path: 'watchlist', loadComponent: () => import('./pages/watchlist.component').then((m) => m.WatchlistComponent) },
  { path: 'portfolio', loadComponent: () => import('./pages/portfolio.component').then((m) => m.PortfolioComponent) },
  { path: 'learn', loadComponent: () => import('./pages/learning-center.component').then((m) => m.LearningCenterComponent) },
  { path: 'learn/:slug', loadComponent: () => import('./pages/learning-center.component').then((m) => m.LearningCenterComponent) },
  { path: 'notifications', loadComponent: () => import('./pages/notifications.component').then((m) => m.NotificationsComponent) },
  { path: 'history', loadComponent: () => import('./pages/prediction-history.component').then((m) => m.PredictionHistoryComponent) },
  { path: 'accuracy', loadComponent: () => import('./pages/model-accuracy.component').then((m) => m.ModelAccuracyComponent) },
  { path: 'upgrade', loadComponent: () => import('./pages/upgrade.component').then((m) => m.UpgradeComponent) },
  { path: 'login', loadComponent: () => import('./auth/login.component').then((m) => m.LoginComponent) },
  { path: 'signup', loadComponent: () => import('./auth/signup.component').then((m) => m.SignupComponent) },
  { path: 'auth/callback', loadComponent: () => import('./auth/auth-callback.component').then((m) => m.AuthCallbackComponent) },
  { path: 'admin/affiliate-stats', loadComponent: () => import('./pages/affiliate-stats.component').then((m) => m.AffiliateStatsComponent) },
  { path: '**', redirectTo: '' },
];
