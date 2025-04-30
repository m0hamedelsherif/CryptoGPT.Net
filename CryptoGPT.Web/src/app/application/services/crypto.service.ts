import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { tap, catchError, finalize, map } from 'rxjs/operators';
import { CryptoApiService } from '../../infrastructure/api/crypto-api.service';
import { AppStateService } from '../state/app-state.service';
import { CryptoCurrency, CryptoCurrencyDetail, PriceHistoryPoint } from '../../domain/models/crypto-currency.model';

@Injectable({
  providedIn: 'root'
})
export class CryptoService {
  // Use the state observables from the AppStateService
  coins$ = this.appState.coins$;
  marketOverview$ = this.appState.marketOverview$;
  loading$ = this.appState.marketLoading$;
  error$ = this.appState.marketError$;

  constructor(
    private cryptoApiService: CryptoApiService,
    private appState: AppStateService
  ) {}

  loadCoins(): Observable<CryptoCurrency[]> {
    this.appState.setMarketLoading(true);
    return this.cryptoApiService.getCoins().pipe(
      tap(coins => this.appState.setCoins(coins)),
      catchError(error => {
        console.error('Error loading coins', error);
        this.appState.setMarketError('Failed to load cryptocurrency data');
        return of([]);
      }),
      finalize(() => this.appState.setMarketLoading(false))
    );
  }

  loadMarketOverview(): Observable<any> {
    this.appState.setMarketLoading(true);
    return this.cryptoApiService.getMarketOverview().pipe(
      tap(overview => this.appState.setMarketOverview(overview)),
      catchError(error => {
        console.error('Error loading market overview', error);
        this.appState.setMarketError('Failed to load market overview data');
        return of(null);
      }),
      finalize(() => this.appState.setMarketLoading(false))
    );
  }

  getCoinDetail(id: string): Observable<CryptoCurrencyDetail> {
    this.appState.setSelectedCoin(id);
    return this.cryptoApiService.getCoinDetail(id);
  }

  getPriceHistory(id: string, days: number): Observable<PriceHistoryPoint[]> {
    return this.cryptoApiService.getPriceHistory(id, days);
  }

  getTopPerformers(count: number = 5): Observable<CryptoCurrency[]> {
    // This can be implemented using the state directly
    return this.coins$.pipe(
      map(coins => {
        return [...coins]
          .sort((a, b) => b.changePercentage24h - a.changePercentage24h)
          .slice(0, count);
      })
    );
  }

  getWorstPerformers(count: number = 5): Observable<CryptoCurrency[]> {
    // This can be implemented using the state directly
    return this.coins$.pipe(
      map(coins => {
        return [...coins]
          .sort((a, b) => a.changePercentage24h - b.changePercentage24h)
          .slice(0, count);
      })
    );
  }
}