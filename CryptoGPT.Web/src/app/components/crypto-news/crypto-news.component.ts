import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NewsService } from '../../services/news.service';
import { CryptoDataService } from '../../services/crypto-data.service';

@Component({
  selector: 'app-crypto-news',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './crypto-news.component.html',
  styleUrls: ['./crypto-news.component.css']
})
export class CryptoNewsComponent implements OnInit {
  isLoading = true;
  isLoadingMore = false;
  error: string | null = null;
  searchTerm = '';
  selectedCoin = '';
  
  // News data
  allNews: any[] = [];
  filteredNews: any[] = [];
  topCoins: any[] = [];
  currentPage = 1;
  pageSize = 15;
  hasMoreNews = true;

  constructor(
    private newsService: NewsService,
    private cryptoService: CryptoDataService
  ) {}

  ngOnInit(): void {
    this.loadInitialData();
  }

  loadInitialData(): void {
    this.isLoading = true;
    this.error = null;

    // Load top cryptocurrencies for the filter dropdown
    this.cryptoService.getTopCoins(20).subscribe({
      next: (data) => {
        this.topCoins = data;
        this.loadMarketNews();
      },
      error: (error) => {
        console.error('Error fetching top coins:', error);
        this.error = 'Failed to load cryptocurrency list.';
        this.isLoading = false;
      }
    });
  }

  loadMarketNews(): void {
    this.newsService.getMarketNews(this.pageSize).subscribe({
      next: (data) => {
        this.allNews = data;
        this.filterNews();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error fetching news:', error);
        this.error = 'Failed to load news articles.';
        this.isLoading = false;
      }
    });
  }

  loadCoinNews(): void {
    if (!this.selectedCoin) {
      this.loadMarketNews();
      return;
    }

    this.isLoading = true;
    const selectedCryptoData = this.topCoins.find(coin => coin.id === this.selectedCoin);

    this.newsService.getCoinNews(this.selectedCoin, selectedCryptoData?.symbol, this.pageSize).subscribe({
      next: (data) => {
        this.allNews = data;
        this.filterNews();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error fetching coin news:', error);
        this.error = 'Failed to load news articles.';
        this.isLoading = false;
      }
    });
  }

  filterNews(): void {
    if (!this.searchTerm) {
      this.filteredNews = this.allNews;
      return;
    }

    const searchLower = this.searchTerm.toLowerCase();
    this.filteredNews = this.allNews.filter(article => 
      article.title.toLowerCase().includes(searchLower) ||
      article.description.toLowerCase().includes(searchLower) ||
      article.tags?.some((tag: string) => tag.toLowerCase().includes(searchLower))
    );
  }

  loadMoreNews(): void {
    this.isLoadingMore = true;
    this.currentPage++;
    
    const loadMore$ = this.selectedCoin ? 
      this.newsService.getCoinNews(this.selectedCoin, this.topCoins.find(coin => coin.id === this.selectedCoin)?.symbol, this.pageSize) :
      this.newsService.getMarketNews(this.pageSize);

    loadMore$.subscribe({
      next: (data) => {
        if (data.length < this.pageSize) {
          this.hasMoreNews = false;
        }
        this.allNews = [...this.allNews, ...data];
        this.filterNews();
        this.isLoadingMore = false;
      },
      error: (error) => {
        console.error('Error loading more news:', error);
        this.error = 'Failed to load more articles.';
        this.isLoadingMore = false;
      }
    });
  }
}
