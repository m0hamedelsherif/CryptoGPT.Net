import { CryptoCurrency, CryptoNewsItem, MarketOverview, RiskProfile } from '../../domain/models/crypto-currency.model';

/**
 * Application state interface that defines the overall state structure
 * following Clean Architecture principles
 */
export interface AppState {
  market: MarketState;
  news: NewsState;
  recommendations: RecommendationsState;
  ui: UiState;
}

/**
 * Market data state
 */
export interface MarketState {
  coins: CryptoCurrency[];
  overview: MarketOverview | null;
  selectedCoinId: string | null;
  loading: boolean;
  error: string | null;
}

/**
 * News state
 */
export interface NewsState {
  allNews: CryptoNewsItem[];
  filteredNews: CryptoNewsItem[];
  searchTerm: string;
  filter: string;
  loading: boolean;
  error: string | null;
}

/**
 * Recommendations state
 */
export interface RecommendationsState {
  recommendations: CryptoCurrency[];
  userProfile: RiskProfile | null;
  loading: boolean;
  error: string | null;
}

/**
 * UI state for application-wide UI settings
 */
export interface UiState {
  darkMode: boolean;
  sidebarOpen: boolean;
  activePage: string;
}

/**
 * Initial state for the application
 */
export const initialState: AppState = {
  market: {
    coins: [],
    overview: null,
    selectedCoinId: null,
    loading: false,
    error: null
  },
  news: {
    allNews: [],
    filteredNews: [],
    searchTerm: '',
    filter: 'all',
    loading: false,
    error: null
  },
  recommendations: {
    recommendations: [],
    userProfile: null,
    loading: false,
    error: null
  },
  ui: {
    darkMode: false,
    sidebarOpen: true,
    activePage: 'dashboard'
  }
};