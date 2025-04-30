import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { Observable, switchMap, tap, combineLatest } from 'rxjs';
import { CryptoService } from '../../../application/services/crypto.service';
import { NewsService } from '../../../application/services/news.service';
import { CryptoCurrencyDetail, CryptoNewsItem, PriceHistoryPoint } from '../../../domain/models/crypto-currency.model';

@Component({
  selector: 'app-coin-detail',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="coin-detail" *ngIf="coinDetail">
      <div class="header">
        <h1>{{ coinDetail.name }} ({{ coinDetail.symbol }})</h1>
        <div class="price-container">
          <div class="current-price">${{ coinDetail.currentPrice | number:'1.2-6' }}</div>
          <div class="change" 
               [class.positive]="coinDetail.changePercentage24h > 0"
               [class.negative]="coinDetail.changePercentage24h < 0">
            {{ coinDetail.changePercentage24h | number:'1.2-2' }}% (24h)
          </div>
        </div>
      </div>
      
      <div class="info-grid">
        <div class="info-card">
          <h3>Market Cap</h3>
          <div class="value">${{ coinDetail.marketCap | number }}</div>
        </div>
        <div class="info-card" *ngIf="coinDetail.circulatingSupply">
          <h3>Circulating Supply</h3>
          <div class="value">{{ coinDetail.circulatingSupply | number }}</div>
        </div>
        <div class="info-card" *ngIf="coinDetail.totalSupply">
          <h3>Total Supply</h3>
          <div class="value">{{ coinDetail.totalSupply | number }}</div>
        </div>
        <div class="info-card" *ngIf="coinDetail.allTimeHigh">
          <h3>All Time High</h3>
          <div class="value">${{ coinDetail.allTimeHigh | number:'1.2-2' }}</div>
          <div class="sub-value" *ngIf="coinDetail.allTimeHighDate">
            {{ coinDetail.allTimeHighDate | date }}
          </div>
        </div>
      </div>
      
      <div class="description" *ngIf="coinDetail.description">
        <h2>About {{ coinDetail.name }}</h2>
        <p>{{ coinDetail.description }}</p>
      </div>
      
      <div class="technical-analysis" *ngIf="coinDetail.technicalAnalysis">
        <h2>Technical Analysis</h2>
        <div class="ta-grid">
          <div class="ta-card">
            <h3>Trend</h3>
            <div class="value"
                 [class.positive]="coinDetail.technicalAnalysis.trend === 'bullish'"
                 [class.negative]="coinDetail.technicalAnalysis.trend === 'bearish'"
                 [class.neutral]="coinDetail.technicalAnalysis.trend === 'neutral'">
              {{ coinDetail.technicalAnalysis.trend | titlecase }}
            </div>
          </div>
          <div class="ta-card">
            <h3>Signal Strength</h3>
            <div class="value">{{ coinDetail.technicalAnalysis.signalStrength }}/10</div>
          </div>
          <div class="ta-card" *ngIf="coinDetail.technicalAnalysis.indicators?.rsi">
            <h3>RSI</h3>
            <div class="value">{{ coinDetail.technicalAnalysis.indicators.rsi }}</div>
          </div>
        </div>
        
        <div class="levels">
          <div class="support">
            <h3>Support Levels</h3>
            <ul>
              <li *ngFor="let level of coinDetail.technicalAnalysis.supportLevels">
                ${{ level | number:'1.2-2' }}
              </li>
            </ul>
          </div>
          <div class="resistance">
            <h3>Resistance Levels</h3>
            <ul>
              <li *ngFor="let level of coinDetail.technicalAnalysis.resistanceLevels">
                ${{ level | number:'1.2-2' }}
              </li>
            </ul>
          </div>
        </div>
      </div>
      
      <div class="price-history">
        <h2>Price History</h2>
        <div class="chart-container">
          <!-- Chart would be implemented here, typically with a library like Chart.js or ngx-charts -->
          <div class="chart-placeholder">Price Chart Visualization</div>
        </div>
      </div>
      
      <div class="related-news" *ngIf="news.length > 0">
        <h2>Related News</h2>
        <div class="news-item" *ngFor="let newsItem of news">
          <h3>{{ newsItem.title }}</h3>
          <p>{{ newsItem.summary }}</p>
          <div class="meta">
            <span>{{ newsItem.source }}</span>
            <span>{{ newsItem.publishedAt | date }}</span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
    .coin-detail {
      padding: 1rem;
      max-width: 1200px;
      margin: 0 auto;
    }
    
    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
    }
    
    .current-price {
      font-size: 2rem;
      font-weight: bold;
    }
    
    .change {
      font-size: 1.2rem;
    }
    
    .positive {
      color: green;
    }
    
    .negative {
      color: red;
    }
    
    .neutral {
      color: orange;
    }
    
    .info-grid, .ta-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 1rem;
      margin-bottom: 2rem;
    }
    
    .info-card, .ta-card {
      background-color: #f5f5f5;
      border-radius: 8px;
      padding: 1rem;
    }
    
    .value {
      font-size: 1.4rem;
      font-weight: bold;
    }
    
    .sub-value {
      font-size: 0.9rem;
      color: #666;
    }
    
    .description {
      margin-bottom: 2rem;
    }
    
    .levels {
      display: flex;
      gap: 2rem;
      margin-top: 1rem;
    }
    
    .chart-container {
      height: 400px;
      margin-bottom: 2rem;
    }
    
    .chart-placeholder {
      height: 100%;
      border: 1px dashed #ccc;
      display: flex;
      justify-content: center;
      align-items: center;
      color: #999;
    }
    
    .news-item {
      margin-bottom: 1rem;
      padding-bottom: 1rem;
      border-bottom: 1px solid #eee;
    }
    
    .meta {
      display: flex;
      gap: 1rem;
      font-size: 0.9rem;
      color: #666;
    }
    `
  ]
})
export class CoinDetailComponent implements OnInit {
  coinId: string = '';
  coinDetail: CryptoCurrencyDetail | null = null;
  priceHistory: PriceHistoryPoint[] = [];
  news: CryptoNewsItem[] = [];
  
  constructor(
    private route: ActivatedRoute,
    private cryptoService: CryptoService,
    private newsService: NewsService
  ) {}
  
  ngOnInit(): void {
    this.route.paramMap.pipe(
      tap(params => {
        const id = params.get('id');
        if (id) {
          this.coinId = id;
        }
      }),
      switchMap(() => this.loadData())
    ).subscribe();
  }
  
  loadData(): Observable<[CryptoCurrencyDetail, PriceHistoryPoint[], CryptoNewsItem[]]> {
    return combineLatest([
      this.cryptoService.getCoinDetail(this.coinId),
      this.cryptoService.getPriceHistory(this.coinId, 30),
      this.newsService.loadNewsForCoin(this.coinId, 5)
    ]).pipe(
      tap(([detail, history, news]) => {
        this.coinDetail = detail;
        this.priceHistory = history;
        this.news = news;
      })
    );
  }
}