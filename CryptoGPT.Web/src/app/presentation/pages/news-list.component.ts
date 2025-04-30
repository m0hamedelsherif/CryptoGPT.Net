import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { NewsService } from '../../../application/services/news.service';
import { CryptoNewsItem } from '../../../domain/models/crypto-currency.model';

@Component({
  selector: 'app-news-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="news-page">
      <h1>Cryptocurrency News</h1>
      
      <div class="filter-bar">
        <input type="text" placeholder="Search news..." (input)="onSearch($event)">
        <select (change)="onFilterChange($event)">
          <option value="all">All Sources</option>
          <option value="coindesk">CoinDesk</option>
          <option value="cointelegraph">CoinTelegraph</option>
          <option value="bitcoin-magazine">Bitcoin Magazine</option>
        </select>
      </div>
      
      <div class="news-list">
        <div class="news-item" *ngFor="let newsItem of filteredNews">
          <div class="news-content">
            <h2>{{ newsItem.title }}</h2>
            <p class="summary">{{ newsItem.summary }}</p>
            <div class="meta">
              <span class="source">{{ newsItem.source }}</span>
              <span class="date">{{ newsItem.publishedAt | date }}</span>
              <span class="sentiment" *ngIf="newsItem.sentiment"
                    [class.positive]="newsItem.sentiment === 'positive'"
                    [class.negative]="newsItem.sentiment === 'negative'"
                    [class.neutral]="newsItem.sentiment === 'neutral'">
                {{ newsItem.sentiment | titlecase }}
              </span>
            </div>
            <div class="tags" *ngIf="newsItem.relatedCoins?.length">
              <span class="tag" *ngFor="let coin of newsItem.relatedCoins">{{ coin }}</span>
            </div>
          </div>
          <a [href]="newsItem.url" target="_blank" class="read-more">Read More</a>
        </div>
      </div>
      
      <div *ngIf="filteredNews.length === 0" class="no-results">
        <p>No news articles found matching your criteria.</p>
      </div>
      
      <button *ngIf="hasMoreNews" class="load-more" (click)="loadMoreNews()">
        Load More News
      </button>
    </div>
  `,
  styles: [
    `
    .news-page {
      max-width: 1000px;
      margin: 0 auto;
      padding: 1rem;
    }
    
    .filter-bar {
      display: flex;
      gap: 1rem;
      margin-bottom: 2rem;
    }
    
    .filter-bar input, .filter-bar select {
      padding: 0.5rem;
      border: 1px solid #ccc;
      border-radius: 4px;
    }
    
    .filter-bar input {
      flex-grow: 1;
    }
    
    .news-item {
      border-bottom: 1px solid #eee;
      padding: 1.5rem 0;
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
    }
    
    .news-content {
      flex: 1;
    }
    
    h2 {
      margin-top: 0;
      margin-bottom: 0.5rem;
      font-size: 1.4rem;
    }
    
    .summary {
      color: #555;
      margin-bottom: 1rem;
    }
    
    .meta {
      display: flex;
      gap: 1rem;
      margin-bottom: 0.5rem;
      font-size: 0.9rem;
      color: #666;
    }
    
    .sentiment {
      padding: 0.2rem 0.5rem;
      border-radius: 4px;
    }
    
    .positive {
      background-color: rgba(0, 128, 0, 0.1);
      color: green;
    }
    
    .negative {
      background-color: rgba(255, 0, 0, 0.1);
      color: red;
    }
    
    .neutral {
      background-color: rgba(255, 165, 0, 0.1);
      color: orange;
    }
    
    .tags {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
    }
    
    .tag {
      background-color: #f0f0f0;
      padding: 0.2rem 0.5rem;
      border-radius: 4px;
      font-size: 0.8rem;
    }
    
    .read-more {
      background-color: #0066cc;
      color: white;
      padding: 0.5rem 1rem;
      border-radius: 4px;
      text-decoration: none;
      white-space: nowrap;
      margin-left: 1rem;
    }
    
    .load-more {
      display: block;
      margin: 2rem auto 0;
      background-color: #f0f0f0;
      border: none;
      padding: 0.75rem 2rem;
      border-radius: 4px;
      cursor: pointer;
      font-weight: bold;
    }
    
    .no-results {
      text-align: center;
      padding: 3rem 0;
      color: #666;
    }
    `
  ]
})
export class NewsListComponent implements OnInit {
  allNews: CryptoNewsItem[] = [];
  filteredNews: CryptoNewsItem[] = [];
  currentFilter = 'all';
  searchTerm = '';
  hasMoreNews = true;
  
  constructor(private newsService: NewsService) {}
  
  ngOnInit(): void {
    this.loadInitialNews();
  }
  
  loadInitialNews(): void {
    this.newsService.loadNews(20).subscribe(news => {
      this.allNews = news;
      this.applyFilters();
    });
  }
  
  loadMoreNews(): void {
    // In a real app, would implement pagination or infinite scroll
    // Here we're just simulating loading more news
    this.newsService.loadNews(10).subscribe(news => {
      if (news.length === 0) {
        this.hasMoreNews = false;
      } else {
        this.allNews = [...this.allNews, ...news];
        this.applyFilters();
      }
    });
  }
  
  onSearch(event: Event): void {
    this.searchTerm = (event.target as HTMLInputElement).value.toLowerCase();
    this.applyFilters();
  }
  
  onFilterChange(event: Event): void {
    this.currentFilter = (event.target as HTMLSelectElement).value;
    this.applyFilters();
  }
  
  applyFilters(): void {
    this.filteredNews = this.allNews.filter(news => {
      const matchesSearch = 
        news.title.toLowerCase().includes(this.searchTerm) || 
        news.summary.toLowerCase().includes(this.searchTerm);
        
      const matchesFilter = 
        this.currentFilter === 'all' || 
        news.source.toLowerCase().includes(this.currentFilter);
        
      return matchesSearch && matchesFilter;
    });
  }
}