import { Component, OnInit, Signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { NewsService } from '../../../application/services/news.service';
import { CryptoNewsItem } from '../../../domain/models/crypto-currency.model';
import { NewsCardComponent } from '../../components/news-card/news-card.component';

@Component({
  selector: 'app-news-list',
  standalone: true,
  imports: [CommonModule, RouterModule, NewsCardComponent],
  templateUrl: './news-list.component.html',
  styleUrls: ['./news-list.component.scss'],
})
export class NewsListComponent implements OnInit {
  private newsService = inject(NewsService);

  news: Signal<CryptoNewsItem[]> = this.newsService.news;
  loading: Signal<boolean> = this.newsService.loading;
  error: Signal<string | null> = this.newsService.error;
  searchTerm: Signal<string> = this.newsService.searchTerm;
  filter: Signal<string> = this.newsService.filter;
  
  filteredNews: Signal<CryptoNewsItem[]> = computed(() => this.news());
  hasMoreNews: boolean = true;

  constructor() {}

  ngOnInit(): void {
    if (this.news().length === 0) {
      this.loadInitialNews();
    }
  }

  loadInitialNews(): void {
    this.newsService.loadNews(20).subscribe();
  }

  onSearch(event: Event): void {
    const term = (event.target as HTMLInputElement).value;
    this.newsService.setSearchTerm(term);
  }

  onFilterChange(event: Event): void {
    const filterValue = (event.target as HTMLSelectElement).value;
    this.newsService.setFilter(filterValue);
  }
  
  loadMoreNews(): void {
    this.newsService.loadMoreNews(10).subscribe({
      next: (hasMore) => {
        this.hasMoreNews = hasMore;
      },
      error: (err) => {
        console.error('Error loading more news:', err);
        this.hasMoreNews = false;
      }
    });
  }
}