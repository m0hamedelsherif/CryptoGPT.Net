import { Injectable, signal, computed, WritableSignal, Signal } from '@angular/core';
import { AppState, initialState } from './app-state.model';
import { CryptoCurrency, CryptoCurrencyDetail, CryptoNewsItem, MarketOverview, RiskProfile } from '../../domain/models/crypto-currency.model';

/**
 * Application state service that manages the entire application state
 * using Angular Signals.
 */
@Injectable({
  providedIn: 'root'
})
export class AppStateService {
  // The main signal containing the entire app state
  private state: WritableSignal<AppState> = signal(initialState);

  // Expose the whole state as a readonly signal if needed (optional)
  readonly stateSignal: Signal<AppState> = this.state.asReadonly();

  // Market state selectors as computed signals
  readonly coins: Signal<CryptoCurrency[]> = computed(() => this.state().market.coins);
  readonly marketOverview: Signal<MarketOverview | null> = computed(() => this.state().market.overview);
  readonly marketLoading: Signal<boolean> = computed(() => this.state().market.loading);
  readonly marketError: Signal<string | null> = computed(() => this.state().market.error);
  readonly selectedCoinId: Signal<string | null> = computed(() => this.state().market.selectedCoinId);

  // News state selectors as computed signals
  readonly news: Signal<CryptoNewsItem[]> = computed(() => this.state().news.filteredNews);
  readonly newsLoading: Signal<boolean> = computed(() => this.state().news.loading);
  readonly newsError: Signal<string | null> = computed(() => this.state().news.error);
  readonly newsSearchTerm: Signal<string> = computed(() => this.state().news.searchTerm);
  readonly newsFilter: Signal<string> = computed(() => this.state().news.filter);

  // Recommendations state selectors as computed signals
  readonly recommendations: Signal<CryptoCurrency[]> = computed(() => this.state().recommendations.recommendations);
  readonly userProfile: Signal<RiskProfile | null> = computed(() => this.state().recommendations.userProfile);
  readonly recommendationsLoading: Signal<boolean> = computed(() => this.state().recommendations.loading);
  readonly recommendationsError: Signal<string | null> = computed(() => this.state().recommendations.error);

  // UI state selectors as computed signals
  readonly darkMode: Signal<boolean> = computed(() => this.state().ui.darkMode);
  readonly activePage: Signal<string> = computed(() => this.state().ui.activePage);

  constructor() {}

  /**
   * Updates the market coins data in the state
   */
  setCoins(coins: CryptoCurrency[]): void {
    this.state.update(current => ({
      ...current,
      market: {
        ...current.market,
        coins,
        loading: false,
        error: null
      }
    }));
  }

  /**
   * Sets the loading state for market data
   */
  setMarketLoading(loading: boolean): void {
    this.state.update(current => ({
      ...current,
      market: {
        ...current.market,
        loading
      }
    }));
  }

  /**
   * Sets the market error state
   */
  setMarketError(error: string | null): void {
    this.state.update(current => ({
      ...current,
      market: {
        ...current.market,
        error,
        loading: false
      }
    }));
  }

  /**
   * Updates the market overview data in the state
   */
  setMarketOverview(overview: MarketOverview | null): void {
    this.state.update(current => ({
      ...current,
      market: {
        ...current.market,
        overview,
        loading: false,
        error: null
      }
    }));
  }

  /**
   * Sets the selected coin ID
   */
  setSelectedCoin(coinId: string | null): void {
    this.state.update(current => ({
      ...current,
      market: {
        ...current.market,
        selectedCoinId: coinId
      }
    }));
  }

  /**
   * Updates the news data in the state
   */
  setAllNews(allNews: CryptoNewsItem[]): void {
    const currentNewsState = this.state().news;
    this.state.update(current => ({
      ...current,
      news: {
        ...currentNewsState,
        allNews,
        filteredNews: this.filterNews(allNews, currentNewsState.searchTerm, currentNewsState.filter),
        loading: false,
        error: null
      }
    }));
  }

  /**
   * Updates news search term and filtered results
   */
  setNewsSearchTerm(searchTerm: string): void {
    const currentNewsState = this.state().news;
    this.state.update(current => ({
      ...current,
      news: {
        ...currentNewsState,
        searchTerm,
        filteredNews: this.filterNews(
          currentNewsState.allNews,
          searchTerm,
          currentNewsState.filter
        )
      }
    }));
  }

  /**
   * Updates news filter and filtered results
   */
  setNewsFilter(filter: string): void {
    const currentNewsState = this.state().news;
    this.state.update(current => ({
      ...current,
      news: {
        ...currentNewsState,
        filter,
        filteredNews: this.filterNews(
          currentNewsState.allNews,
          currentNewsState.searchTerm,
          filter
        )
      }
    }));
  }

  /**
   * Sets the news loading state
   */
  setNewsLoading(loading: boolean): void {
    this.state.update(current => ({
      ...current,
      news: {
        ...current.news,
        loading
      }
    }));
  }

  /**
   * Sets the news error state
   */
  setNewsError(error: string | null): void {
    this.state.update(current => ({
      ...current,
      news: {
        ...current.news,
        error,
        loading: false
      }
    }));
  }

  /**
   * Sets the recommendations in the state
   */
  setRecommendations(recommendations: CryptoCurrency[]): void {
    this.state.update(current => ({
      ...current,
      recommendations: {
        ...current.recommendations,
        recommendations,
        loading: false,
        error: null
      }
    }));
  }

  /**
   * Sets the recommendations loading state
   */
  setRecommendationsLoading(loading: boolean): void {
    this.state.update(current => ({
      ...current,
      recommendations: {
        ...current.recommendations,
        loading
      }
    }));
  }

  /**
   * Sets the recommendations error state
   */
  setRecommendationsError(error: string | null): void {
    this.state.update(current => ({
      ...current,
      recommendations: {
        ...current.recommendations,
        error,
        loading: false
      }
    }));
  }

  /**
   * Sets the user risk profile
   */
  setUserProfile(userProfile: RiskProfile | null): void {
    this.state.update(current => ({
      ...current,
      recommendations: {
        ...current.recommendations,
        userProfile
      }
    }));
  }

  /**
   * Toggles dark mode
   */
  toggleDarkMode(): void {
    this.state.update(current => ({
      ...current,
      ui: {
        ...current.ui,
        darkMode: !current.ui.darkMode
      }
    }));
    const newDarkMode = this.state().ui.darkMode;
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
    this.state.update(current => ({
      ...current,
      ui: {
        ...current.ui,
        activePage: page
      }
    }));
  }

  /**
   * Helper method to filter news based on search term and filter
   */
  private filterNews(news: CryptoNewsItem[], searchTerm: string, filter: string): CryptoNewsItem[] {
    return news.filter(item => {
      const matchesSearch = !searchTerm ||
        (item?.title?.toLowerCase() || '').includes(searchTerm.toLowerCase()) ||
        (item?.summary?.toLowerCase() || '').includes(searchTerm.toLowerCase());

      const matchesFilter = filter === 'all' ||
        (item.source.toLowerCase() || '').includes(filter.toLowerCase());

      return matchesSearch && matchesFilter;
    });
  }
}