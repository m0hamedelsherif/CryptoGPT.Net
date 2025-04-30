import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CryptoService } from '../../../application/services/crypto.service';
import { NewsService } from '../../../application/services/news.service';
import { CryptoCurrency, MarketOverview, CryptoNewsItem } from '../../../domain/models/crypto-currency.model';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="dashboard">
      <h1>Crypto Dashboard</h1>
      
      <div class="market-overview" *ngIf="marketOverview$ | async as overview">
        <h2>Market Overview</h2>
        <div class="stats-grid">
          <div class="stat-box">
            <span class="label">Total Market Cap</span>
            <span class="value">${{ overview.totalMarketCap | number }}</span>
          </div>
          <div class="stat-box">
            <span class="label">24h Volume</span>
            <span class="value">${{ overview.totalVolume24h | number }}</span>
          </div>
          <div class="stat-box">
            <span class="label">BTC Dominance</span>
            <span class="value">{{ overview.btcDominance | number:'1.2-2' }}%</span>
          </div>
          <div class="stat-box">
            <span class="label">Market Sentiment</span>
            <span class="value">{{ overview.marketSentiment }}</span>
          </div>
        </div>
      </div>
      
      <div class="top-performers" *ngIf="topPerformers$ | async as topPerformers">
        <h2>Top Performers (24h)</h2>
        <div class="performers-grid">
          <div class="performer-card" *ngFor="let coin of topPerformers" [routerLink]="['/coin', coin.id]">
            <div class="name">{{ coin.name }} ({{ coin.symbol }})</div>
            <div class="change positive">+{{ coin.changePercentage24h | number:'1.2-2' }}%</div>
            <div class="price">${{ coin.currentPrice | number:'1.2-6' }}</div>
          </div>
        </div>
      </div>
      
      <div class="latest-news" *ngIf="news$ | async as news">
        <h2>Latest News</h2>
        <div class="news-item" *ngFor="let newsItem of news" [routerLink]="['/news', newsItem.id]">
          <h3>{{ newsItem.title }}</h3>
          <p class="summary">{{ newsItem.summary }}</p>
          <div class="meta">
            <span>{{ newsItem.source }}</span>
            <span>{{ newsItem.publishedAt | date }}</span>
            <span class="sentiment" 
                 [class.positive]="newsItem.sentiment === 'positive'"
                 [class.negative]="newsItem.sentiment === 'negative'"
                 [class.neutral]="newsItem.sentiment === 'neutral'">
              {{ newsItem.sentiment }}
            </span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
    .dashboard {
      padding: 1rem;
    }
    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 1rem;
      margin-bottom: 2rem;
    }
    .stat-box {
      background-color: #f5f5f5;
      border-radius: 8px;
      padding: 1rem;
      display: flex;
      flex-direction: column;
    }
    .stat-box .label {
      font-size: 0.9rem;
      color: #666;
    }
    .stat-box .value {
      font-size: 1.4rem;
      font-weight: bold;
    }
    .performers-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 1rem;
      margin-bottom: 2rem;
    }
    .performer-card {
      background-color: #f5f5f5;
      border-radius: 8px;
      padding: 1rem;
      cursor: pointer;
    }
    .performer-card:hover {
      background-color: #eaeaea;
    }
    .news-item {
      border-bottom: 1px solid #eee;
      padding: 1rem 0;
      cursor: pointer;
    }
    .news-item:hover {
      background-color: #f9f9f9;
    }
    .news-item .summary {
      color: #555;
    }
    .news-item .meta {
      display: flex;
      gap: 1rem;
      font-size: 0.9rem;
      color: #777;
    }
    .sentiment.positive {
      color: green;
    }
    .sentiment.negative {
      color: red;
    }
    .sentiment.neutral {
      color: orange;
    }
    `
  ]
})
export class DashboardComponent implements OnInit {
  marketOverview$: Observable<MarketOverview | null>;
  topPerformers$: Observable<CryptoCurrency[]>;
  news$: Observable<CryptoNewsItem[]>;
  
  constructor(
    private cryptoService: CryptoService,
    private newsService: NewsService
  ) {
    this.marketOverview$ = this.cryptoService.marketOverview$;
    this.topPerformers$ = this.cryptoService.getTopPerformers(5);
    this.news$ = this.newsService.news$;
  }
  
  ngOnInit(): void {
    this.cryptoService.loadMarketOverview().subscribe();
    this.cryptoService.loadCoins().subscribe();
    this.newsService.loadNews(5).subscribe();
  }
}