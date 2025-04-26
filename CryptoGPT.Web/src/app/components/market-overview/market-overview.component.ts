import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CryptoDataService } from '../../services/crypto-data.service';

@Component({
  selector: 'app-market-overview',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './market-overview.component.html',
  styleUrls: ['./market-overview.component.css']
})
export class MarketOverviewComponent implements OnInit {
  isLoading = true;
  marketOverview: any;
  allCoins: any[] = [];
  filteredCoins: any[] = [];
  topGainers: any[] = [];
  topLosers: any[] = [];
  marketSentiment = 'Neutral';
  
  // Search and sort parameters
  searchTerm = '';
  sortBy = 'marketCapRank';
  sortDirection: 'asc' | 'desc' = 'asc';

  constructor(private cryptoService: CryptoDataService) {}

  ngOnInit(): void {
    this.loadMarketData();
  }

  loadMarketData(): void {
    // Load market overview data
    this.cryptoService.getMarketOverview().subscribe({
      next: (data) => {
        this.marketOverview = data;
        this.topGainers = data.topGainers || [];
        this.topLosers = data.topLosers || [];
        this.calculateMarketSentiment();
      },
      error: (error) => {
        console.error('Error fetching market overview:', error);
      }
    });

    // Load full cryptocurrency list
    this.cryptoService.getTopCoins(100).subscribe({
      next: (data) => {
        this.allCoins = data;
        this.applyFilters();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error fetching coins:', error);
        this.isLoading = false;
      }
    });
  }

  applyFilters(): void {
    // Apply search filter
    let filtered = this.allCoins;
    if (this.searchTerm) {
      const searchLower = this.searchTerm.toLowerCase();
      filtered = filtered.filter(coin => 
        coin.name.toLowerCase().includes(searchLower) ||
        coin.symbol.toLowerCase().includes(searchLower)
      );
    }

    // Apply sorting
    filtered.sort((a, b) => {
      let comparison = 0;
      
      switch(this.sortBy) {
        case 'marketCapRank':
          comparison = a.marketCapRank - b.marketCapRank;
          break;
        case 'currentPrice':
          comparison = a.currentPrice - b.currentPrice;
          break;
        case 'priceChangePercentage24h':
          comparison = a.priceChangePercentage24h - b.priceChangePercentage24h;
          break;
        case 'marketCap':
          comparison = a.marketCap - b.marketCap;
          break;
        case 'volume24h':
          comparison = a.volume24h - b.volume24h;
          break;
      }

      return this.sortDirection === 'asc' ? comparison : -comparison;
    });

    this.filteredCoins = filtered;
  }

  setSortDirection(direction: 'asc' | 'desc'): void {
    this.sortDirection = direction;
    this.applyFilters();
  }

  calculateMarketSentiment(): void {
    if (!this.allCoins || this.allCoins.length === 0) {
      this.marketSentiment = 'Neutral';
      return;
    }

    // Calculate sentiment based on the top 20 cryptocurrencies
    const topCoins = this.allCoins.slice(0, 20);
    const gainers = topCoins.filter(coin => coin.priceChangePercentage24h > 0).length;
    const losers = topCoins.filter(coin => coin.priceChangePercentage24h < 0).length;
    
    if (gainers > losers + 5) {
      this.marketSentiment = 'Bullish';
    } else if (losers > gainers + 5) {
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
