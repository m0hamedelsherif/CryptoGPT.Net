import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./components/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'market',
    loadComponent: () => import('./components/market-overview/market-overview.component').then(m => m.MarketOverviewComponent)
  },
  {
    path: 'news',
    loadComponent: () => import('./components/crypto-news/crypto-news.component').then(m => m.CryptoNewsComponent)
  },
  {
    path: 'recommendations',
    loadComponent: () => import('./components/recommendations/recommendations.component').then(m => m.RecommendationsComponent)
  },
  {
    path: 'coin/:id',
    loadComponent: () => import('./components/coin-detail/coin-detail.component').then(m => m.CoinDetailComponent)
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
