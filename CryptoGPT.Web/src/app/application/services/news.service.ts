import { Injectable, Signal } from '@angular/core'; // Import Signal
import { Observable, of } from 'rxjs';
import { tap, catchError, finalize, map } from 'rxjs/operators';
import { NewsApiService } from '../../infrastructure/api/news-api.service';
import { AppStateService } from '../state/app-state.service';
import { CryptoNewsItem } from '../../domain/models/crypto-currency.model';

@Injectable({
  providedIn: 'root'
})
export class NewsService {
  // Use the signals from the AppStateService
  news: Signal<CryptoNewsItem[]> = this.appState.news;
  loading: Signal<boolean> = this.appState.newsLoading;
  error: Signal<string | null> = this.appState.newsError;
  // Expose search term and filter signals if needed by components
  searchTerm: Signal<string> = this.appState.newsSearchTerm;
  filter: Signal<string> = this.appState.newsFilter;
  
  private currentPage = 1;

  constructor(
    private newsApiService: NewsApiService,
    private appState: AppStateService
  ) {}

  loadNews(count: number = 10): Observable<CryptoNewsItem[]> { // Keep Observable return for async op
    // Reset page counter when loading initial news
    this.currentPage = 1;
    
    this.appState.setNewsLoading(true);
    this.appState.setNewsError(null); // Clear previous errors

    return this.newsApiService.getNews(count, this.currentPage).pipe(
      tap(news => this.appState.setAllNews(news)),
      catchError(error => {
        console.error('Error loading news', error);
        this.appState.setNewsError('Failed to load news data');
        // Don't call setAllNews on error
        return of([]);
      }),
      finalize(() => this.appState.setNewsLoading(false))
    );
  }
  
  loadMoreNews(count: number = 10): Observable<boolean> {
    this.currentPage++;
    this.appState.setNewsLoading(true);
    
    return this.newsApiService.getNews(count, this.currentPage).pipe(
      map(news => {
        if (news.length > 0) {
          // Append new news items to the existing list
          const currentNews = this.appState.stateSignal().news.allNews;
          this.appState.setAllNews([...currentNews, ...news]);
          // Return true if we got the full page (might be more)
          return news.length >= count;
        } else {
          // No more news available
          return false;
        }
      }),
      catchError(error => {
        console.error('Error loading more news', error);
        this.appState.setNewsError('Failed to load more news');
        return of(false);
      }),
      finalize(() => this.appState.setNewsLoading(false))
    );
  }

  // Keep Observable return for async op
  loadNewsForCoin(coinId: string, count: number = 5): Observable<CryptoNewsItem[]> {
     // Consider adding specific loading/error state for coin-specific news if needed
    return this.newsApiService.getNewsForCoin(coinId, count);
  }

  // Methods to update state via AppStateService
  setSearchTerm(term: string): void {
    this.appState.setNewsSearchTerm(term);
  }

  setFilter(filter: string): void {
    this.appState.setNewsFilter(filter);
  }
}