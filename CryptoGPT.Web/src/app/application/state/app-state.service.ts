import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { distinctUntilChanged, map } from 'rxjs/operators';
import { AppState, initialState } from './app-state.model';
import { CryptoCurrency, CryptoCurrencyDetail, CryptoNewsItem, MarketOverview, RiskProfile } from '../../domain/models/crypto-currency.model';

/**
 * Application state service that manages the entire application state
 * following Clean Architecture principles
 */
@Injectable({
  providedIn: 'root'
})
export class AppStateService {
  // The main BehaviorSubject containing the entire app state
  private state = new BehaviorSubject<AppState>(initialState);
  
  // State observables
  readonly state$ = this.state.asObservable();
  
  // Market state selectors
  readonly coins$ = this.state.pipe(
    map(state => state.market.coins),
    distinctUntilChanged()
  );
  
  readonly marketOverview$ = this.state.pipe(
    map(state => state.market.overview),
    distinctUntilChanged()
  );
  
  readonly marketLoading$ = this.state.pipe(
    map(state => state.market.loading),
    distinctUntilChanged()
  );
  
  readonly marketError$ = this.state.pipe(
    map(state => state.market.error),
    distinctUntilChanged()
  );
  
  // News state selectors
  readonly news$ = this.state.pipe(
    map(state => state.news.filteredNews),
    distinctUntilChanged()
  );
  
  readonly newsLoading$ = this.state.pipe(
    map(state => state.news.loading),
    distinctUntilChanged()
  );
  
  readonly newsError$ = this.state.pipe(
    map(state => state.news.error),
    distinctUntilChanged()
  );
  
  // Recommendations state selectors
  readonly recommendations$ = this.state.pipe(
    map(state => state.recommendations.recommendations),
    distinctUntilChanged()
  );
  
  readonly userProfile$ = this.state.pipe(
    map(state => state.recommendations.userProfile),
    distinctUntilChanged()
  );
  
  // UI state selectors
  readonly darkMode$ = this.state.pipe(
    map(state => state.ui.darkMode),
    distinctUntilChanged()
  );
  
  constructor() {}
  
  /**
   * Updates the market coins data in the state
   */
  setCoins(coins: CryptoCurrency[]): void {
    this.updateState({
      market: {
        ...this.currentState.market,
        coins,
        loading: false,
        error: null
      }
    });
  }
  
  /**
   * Sets the loading state for market data
   */
  setMarketLoading(loading: boolean): void {
    this.updateState({
      market: {
        ...this.currentState.market,
        loading
      }
    });
  }
  
  /**
   * Sets the market error state
   */
  setMarketError(error: string): void {
    this.updateState({
      market: {
        ...this.currentState.market,
        error,
        loading: false
      }
    });
  }
  
  /**
   * Updates the market overview data in the state
   */
  setMarketOverview(overview: MarketOverview): void {
    this.updateState({
      market: {
        ...this.currentState.market,
        overview,
        loading: false,
        error: null
      }
    });
  }
  
  /**
   * Sets the selected coin ID
   */
  setSelectedCoin(coinId: string): void {
    this.updateState({
      market: {
        ...this.currentState.market,
        selectedCoinId: coinId
      }
    });
  }
  
  /**
   * Updates the news data in the state
   */
  setAllNews(allNews: CryptoNewsItem[]): void {
    this.updateState({
      news: {
        ...this.currentState.news,
        allNews,
        filteredNews: this.filterNews(allNews, this.currentState.news.searchTerm, this.currentState.news.filter),
        loading: false,
        error: null
      }
    });
  }
  
  /**
   * Updates news search term and filtered results
   */
  setNewsSearchTerm(searchTerm: string): void {
    this.updateState({
      news: {
        ...this.currentState.news,
        searchTerm,
        filteredNews: this.filterNews(
          this.currentState.news.allNews, 
          searchTerm, 
          this.currentState.news.filter
        )
      }
    });
  }
  
  /**
   * Updates news filter and filtered results
   */
  setNewsFilter(filter: string): void {
    this.updateState({
      news: {
        ...this.currentState.news,
        filter,
        filteredNews: this.filterNews(
          this.currentState.news.allNews, 
          this.currentState.news.searchTerm, 
          filter
        )
      }
    });
  }
  
  /**
   * Sets the news loading state
   */
  setNewsLoading(loading: boolean): void {
    this.updateState({
      news: {
        ...this.currentState.news,
        loading
      }
    });
  }
  
  /**
   * Sets the news error state
   */
  setNewsError(error: string | null): void {
    this.updateState({
      news: {
        ...this.currentState.news,
        error,
        loading: false
      }
    });
  }
  
  /**
   * Sets the recommendations in the state
   */
  setRecommendations(recommendations: CryptoCurrency[]): void {
    this.updateState({
      recommendations: {
        ...this.currentState.recommendations,
        recommendations,
        loading: false,
        error: null
      }
    });
  }
  
  /**
   * Sets the user risk profile
   */
  setUserProfile(userProfile: RiskProfile): void {
    this.updateState({
      recommendations: {
        ...this.currentState.recommendations,
        userProfile
      }
    });
  }
  
  /**
   * Toggles dark mode
   */
  toggleDarkMode(): void {
    const newDarkMode = !this.currentState.ui.darkMode;
    this.updateState({
      ui: {
        ...this.currentState.ui,
        darkMode: newDarkMode
      }
    });
    
    // Apply dark mode to body element
    if (newDarkMode) {
      document.body.classList.add('dark-mode');
    } else {
      document.body.classList.remove('dark-mode');
    }
  }
  
  /**
   * Sets the active page
   */
  setActivePage(page: string): void {
    this.updateState({
      ui: {
        ...this.currentState.ui,
        activePage: page
      }
    });
  }
  
  /**
   * Helper method to filter news based on search term and filter
   */
  private filterNews(news: CryptoNewsItem[], searchTerm: string, filter: string): CryptoNewsItem[] {
    return news.filter(item => {
      const matchesSearch = !searchTerm || 
        item.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
        item.summary.toLowerCase().includes(searchTerm.toLowerCase());
        
      const matchesFilter = filter === 'all' || 
        item.source.toLowerCase().includes(filter.toLowerCase());
        
      return matchesSearch && matchesFilter;
    });
  }
  
  /**
   * Gets the current state snapshot
   */
  get currentState(): AppState {
    return this.state.getValue();
  }
  
  /**
   * Updates the state with partial state changes
   */
  updateState(partialState: Partial<AppState>): void {
    this.state.next({
      ...this.currentState,
      ...partialState
    });
  }
}