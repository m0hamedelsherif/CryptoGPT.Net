import { Routes } from '@angular/router';

export const routes: Routes = [
  { 
    path: '',
    loadComponent: () => import('./presentation/pages/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  { 
    path: 'coins',
    loadComponent: () => import('./presentation/components/crypto-list/crypto-list.component').then(m => m.CryptoListComponent)
  },
  { 
    path: 'coin/:id',
    loadComponent: () => import('./presentation/pages/coin-detail/coin-detail.component').then(m => m.CoinDetailComponent)
  },
  { 
    path: 'recommendations',
    loadComponent: () => import('./presentation/pages/recommendations/recommendations.component').then(m => m.RecommendationsComponent)
  },
  { 
    path: 'news',
    loadComponent: () => import('./presentation/pages/news-list/news-list.component').then(m => m.NewsListComponent)
  },
  { 
    path: '**',
    redirectTo: ''
  }
];