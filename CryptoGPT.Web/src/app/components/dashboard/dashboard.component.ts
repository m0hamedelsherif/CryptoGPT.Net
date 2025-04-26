import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CryptoDataService } from '../../services/crypto-data.service';
import { NewsService } from '../../services/news.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  isLoading = true;
  marketOverview: any;
  topCoins: any[] = [];
  topGainers: any[] = [];
  topLosers: any[] = [];
  latestNews: any[] = [];
  marketSentiment = 'Neutral';

  constructor(
    private cryptoService: CryptoDataService,
    private newsService: NewsService
  ) {}

  ngOnInit(): void {
    this.loadMarketOverview();
    this.loadTopCoins();
    this.loadLatestNews();
  }

  loadMarketOverview(): void {
    this.cryptoService.getMarketOverview().subscribe({
      next: (data) => {
        this.marketOverview = data;
        this.topGainers = data.topGainers || [];
        this.topLosers = data.topLosers || [];
        this.calculateMarketSentiment();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error fetching market overview:', error);
        this.isLoading = false;
      }
    });
  }

  loadTopCoins(): void {
    this.cryptoService.getTopCoins(10).subscribe({
      next: (data) => {
        this.topCoins = data;
      },
      error: (error) => {
        console.error('Error fetching top coins:', error);
      }
    });
  }

  loadLatestNews(): void {
    this.newsService.getMarketNews(6).subscribe({
      next: (data) => {
        this.latestNews = data;
      },
      error: (error) => {
        console.error('Error fetching news:', error);
      }
    });
  }

  calculateMarketSentiment(): void {
    if (!this.marketOverview) {
      this.marketSentiment = 'Neutral';
      return;
    }

    // Count how many of the top 10 coins are up vs down in last 24h
    const gainers = this.topCoins.filter(coin => coin.priceChangePercentage24h > 0).length;
    const losers = this.topCoins.filter(coin => coin.priceChangePercentage24h < 0).length;
    
    if (gainers > losers + 3) {
      this.marketSentiment = 'Bullish';
    } else if (losers > gainers + 3) {
      this.marketSentiment = 'Bearish';
    } else {
      this.marketSentiment = 'Neutral';
    }
  }

  getSentimentClass(): string {
    switch(this.marketSentiment) {
      case 'Bullish':
        return 'text-success';
      case 'Bearish':
        return 'text-danger';
      default:
        return 'text-secondary';
    }
  }

  formatLargeNumber(value: number): string {
    if (!value) return '0';
    
    if (value >= 1e12) {
      return (value / 1e12).toFixed(2) + ' T';
    } else if (value >= 1e9) {
      return (value / 1e9).toFixed(2) + ' B';
    } else if (value >= 1e6) {
      return (value / 1e6).toFixed(2) + ' M';
    } else if (value >= 1e3) {
      return (value / 1e3).toFixed(2) + ' K';
    } else {
      return value.toFixed(2);
    }
  }
}
