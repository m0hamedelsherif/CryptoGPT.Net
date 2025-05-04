import { Component, OnInit, Signal, WritableSignal, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CryptoService } from '../../../application/services/crypto.service';
import { NewsService } from '../../../application/services/news.service';
import { RecommendationService } from '../../../application/services/recommendation.service';
import { 
  CryptoCurrency, 
  MarketOverview, 
  CryptoNewsItem, 
  TradeRecommendation 
} from '../../../domain/models/crypto-currency.model';
import { NewsCardComponent } from '../../components/news-card/news-card.component';
import { FormatLargeNumberPipe } from '../../../shared/pipes/format-large-number.pipe';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, NewsCardComponent, FormatLargeNumberPipe],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  // Market data signals
  marketOverview: Signal<MarketOverview | null>;
  topPerformers: Signal<CryptoCurrency[]>;
  worstPerformers: Signal<CryptoCurrency[]>;
  marketLoading: Signal<boolean>;
  marketError: Signal<string | null>;
  
  // News signals
  news: Signal<CryptoNewsItem[]>;
  newsLoading: Signal<boolean>;
  newsError: Signal<string | null>;
  
  // Recommendations signals
  recommendations: Signal<TradeRecommendation[]>;
  
  // Chart loading state
  chartLoading: WritableSignal<boolean> = signal(false);
  
  // UI state signals
  performersView: WritableSignal<'gainers' | 'losers'> = signal('gainers');
  
  // Computed signal for current performers based on selected view
  currentPerformers = computed(() => {
    return this.performersView() === 'gainers' 
      ? this.topPerformers() 
      : this.worstPerformers();
  });

  constructor(
    private cryptoService: CryptoService,
    private newsService: NewsService,
    private recommendationService: RecommendationService
  ) {
    // Assign signals from services
    this.marketOverview = this.cryptoService.marketOverview;
    this.topPerformers = this.cryptoService.getTopPerformers(5);
    this.worstPerformers = this.cryptoService.getWorstPerformers(5);
    this.marketLoading = this.cryptoService.loading;
    this.marketError = this.cryptoService.error;
    
    this.news = this.newsService.news;
    this.newsLoading = this.newsService.loading;
    this.newsError = this.newsService.error;
    
    this.recommendations = this.recommendationService.recommendations;
  }

  ngOnInit(): void {
    this.loadDashboardData();
  }
  
  // Load all dashboard data
  loadDashboardData(): void {
    this.cryptoService.loadMarketOverview().subscribe();
    this.cryptoService.loadCoins().subscribe();
    this.newsService.loadNews(5).subscribe();
    this.recommendationService.loadRecommendations().subscribe();
    this.loadMarketChart();
  }
  
  // Load the market chart data
  loadMarketChart(): void {
    this.chartLoading.set(true);
    
    // Simulate chart loading - this would be replaced with real chart data loading
    setTimeout(() => {
      this.chartLoading.set(false);
    }, 1500);
  }
  
  // Refresh all dashboard data
  refreshData(): void {
    this.loadDashboardData();
  }
  
  // Refresh only news data
  refreshNews(): void {
    this.newsService.loadNews(5).subscribe();
  }
  
  // Set the performers view (gainers or losers)
  setPerformersView(view: 'gainers' | 'losers'): void {
    this.performersView.set(view);
  }
  
  // Format large numbers for display
  formatLargeNumber(value: number | undefined): string {
    if (value === undefined || value === null) {
      return 'N/A';
    }
    
    if (value >= 1e12) {
      return (value / 1e12).toFixed(2) + 'T';
    }
    if (value >= 1e9) {
      return (value / 1e9).toFixed(2) + 'B';
    }
    if (value >= 1e6) {
      return (value / 1e6).toFixed(2) + 'M';
    }
    if (value >= 1e3) {
      return (value / 1e3).toFixed(2) + 'K';
    }
    return value.toFixed(2);
  }
  
  // Get appropriate sentiment icon based on sentiment value
  getSentimentIcon(sentiment: string | undefined): string {
    if (!sentiment) return 'bi-dash-circle';
    
    switch (sentiment.toLowerCase()) {
      case 'bullish':
        return 'bi-graph-up-arrow';
      case 'bearish':
        return 'bi-graph-down-arrow';
      case 'neutral':
        return 'bi-dash-circle';
      default:
        return 'bi-question-circle';
    }
  }
  
  // Get sentiment value description
  getSentimentValue(sentiment: string | undefined): string {
    if (!sentiment) return 'Neutral';
    
    switch (sentiment.toLowerCase()) {
      case 'bullish':
        return 'Strong Market';
      case 'bearish':
        return 'Weak Market';
      case 'neutral':
        return 'Stable Market';
      default:
        return sentiment;
    }
  }
  
  // Get appropriate icon for recommendation type
  getRecommendationIcon(type: string | undefined): string {
    if (!type) return 'bi-question-circle';
    
    switch (type.toLowerCase()) {
      case 'buy':
        return 'bi-arrow-up-circle';
      case 'sell':
        return 'bi-arrow-down-circle';
      case 'hold':
        return 'bi-dash-circle';
      default:
        return 'bi-question-circle';
    }
  }
}