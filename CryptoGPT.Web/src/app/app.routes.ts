import { Routes } from '@angular/router';

export const routes: Routes = [
  { 
    path: '',
    loadComponent: () => import('./presentation/pages/dashboard.component').then(m => m.DashboardComponent)
  },
  { 
    path: 'coins',
    loadComponent: () => import('./presentation/components/crypto-list.component').then(m => m.CryptoListComponent)
  },
  { 
    path: 'coin/:id',
    loadComponent: () => import('./presentation/pages/coin-detail.component').then(m => m.CoinDetailComponent)
  },
  { 
    path: 'recommendations',
    loadComponent: () => import('./presentation/pages/recommendations.component').then(m => m.RecommendationsComponent)
  },
  { 
    path: 'news',
    loadComponent: () => import('./presentation/pages/news-list.component').then(m => m.NewsListComponent)
  },
  { 
    path: '**',
    redirectTo: ''
  }
];