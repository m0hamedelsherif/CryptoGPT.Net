import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { NewsApiService } from '../../infrastructure/api/news-api.service';
import { AppStateService } from '../state/app-state.service';
import { CryptoNewsItem } from '../../domain/models/crypto-currency.model';

@Injectable({
  providedIn: 'root'
})
export class NewsService {
  // Use the state observables from the AppStateService
  news$ = this.appState.news$;
  loading$ = this.appState.newsLoading$;
  error$ = this.appState.newsError$;

  constructor(
    private newsApiService: NewsApiService,
    private appState: AppStateService
  ) {}

  loadNews(count: number = 10): Observable<CryptoNewsItem[]> {
    // Set loading state through the app state service
    this.appState.setNewsLoading(true);
    
    return this.newsApiService.getNews(count).pipe(
      tap(news => this.appState.setAllNews(news)),
      catchError(error => {
        console.error('Error loading news', error);
        this.appState.setNewsError('Failed to load news data');
        return of([]);
      }),
      finalize(() => this.appState.setNewsLoading(false))
    );
  }

  loadNewsForCoin(coinId: string, count: number = 5): Observable<CryptoNewsItem[]> {
    return this.newsApiService.getNewsForCoin(coinId, count);
  }
  
  setSearchTerm(term: string): void {
    this.appState.setNewsSearchTerm(term);
  }
  
  setFilter(filter: string): void {
    this.appState.setNewsFilter(filter);
  }
}