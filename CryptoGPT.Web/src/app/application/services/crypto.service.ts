import { Injectable, Signal, computed } from '@angular/core'; // Import Signal and computed
import { Observable, of } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { CryptoApiService } from '../../infrastructure/api/crypto-api.service';
import { AppStateService } from '../state/app-state.service';
import { CryptoCurrency, CryptoCurrencyDetail, PriceHistoryPoint, MarketOverview } from '../../domain/models/crypto-currency.model'; // Import MarketOverview

@Injectable({
  providedIn: 'root'
})
export class CryptoService {
  // Use the signals from the AppStateService
  coins: Signal<CryptoCurrency[]> = this.appState.coins;
  marketOverview: Signal<MarketOverview | null> = this.appState.marketOverview;
  loading: Signal<boolean> = this.appState.marketLoading;
  error: Signal<string | null> = this.appState.marketError;

  constructor(
    private cryptoApiService: CryptoApiService,
    private appState: AppStateService
  ) {}

  loadCoins(): Observable<CryptoCurrency[]> { // Keep Observable return for the async operation
    this.appState.setMarketLoading(true);
    this.appState.setMarketError(null); // Clear previous errors
    return this.cryptoApiService.getCoins().pipe(
      tap(coins => this.appState.setCoins(coins)),
      catchError(error => {
        console.error('Error loading coins', error);
        this.appState.setMarketError('Failed to load cryptocurrency data');
        return of([]); // Return empty array or rethrow, depending on desired component behavior
      }),
      finalize(() => this.appState.setMarketLoading(false))
    );
  }

  loadMarketOverview(): Observable<MarketOverview | null> { // Keep Observable return for the async operation
    this.appState.setMarketLoading(true);
    this.appState.setMarketError(null); // Clear previous errors
    return this.cryptoApiService.getMarketOverview().pipe(
      tap(overview => this.appState.setMarketOverview(overview)),
      catchError(error => {
        console.error('Error loading market overview', error);
        this.appState.setMarketError('Failed to load market overview data');
        this.appState.setMarketOverview(null); // Clear overview on error
        return of(null);
      }),
      finalize(() => this.appState.setMarketLoading(false))
    );
  }

  // Keep Observable returns for methods involving async API calls
  getCoinDetail(id: string): Observable<CryptoCurrencyDetail> {
    this.appState.setSelectedCoin(id);
    return this.cryptoApiService.getCoinDetail(id);
  }

  getPriceHistory(id: string, days: number): Observable<PriceHistoryPoint[]> {
    // return this.cryptoApiService.getPriceHistory(id, days);
   return of([]); // Placeholder for actual API call
  }

  // Selector for top performers (e.g., top 5 gainers)
  getTopPerformers(count: number): Signal<CryptoCurrency[]> {
    return computed(() => {
      const marketOverview = this.marketOverview();
      // Handle potential null/undefined values before sorting
      return [...marketOverview?.topPerformers ?? []]
        .sort((a, b) => (b.priceChangePercentage24h ?? -Infinity) - (a.priceChangePercentage24h ?? -Infinity))
        .slice(0, count);
    });
  }

  // Selector for worst performers (e.g., top 5 losers)
  getWorstPerformers(count: number): Signal<CryptoCurrency[]> {
    return computed(() => {
      const marketOverview = this.marketOverview();
      // Handle potential null/undefined values before sorting
      return [...marketOverview?.worstPerformers ?? []]
        .sort((a, b) => (a.priceChangePercentage24h ?? Infinity) - (b.priceChangePercentage24h ?? Infinity))
        .slice(0, count);
    });
  }
}