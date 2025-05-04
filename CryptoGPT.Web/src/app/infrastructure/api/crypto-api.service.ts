import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from './api-client.service';
import { CryptoCurrency, CryptoCurrencyDetail, PriceHistoryPoint, MarketOverview } from '../../domain/models/crypto-currency.model';

@Injectable({
  providedIn: 'root'
})
export class CryptoApiService {
  private baseEndpoint = 'coin';

  constructor(private apiClient: ApiClientService) {}

  getCoins(): Observable<CryptoCurrency[]> {
    return this.apiClient.get<CryptoCurrency[]>(this.baseEndpoint);
  }

  getCoinDetail(id: string): Observable<CryptoCurrencyDetail> {
    return this.apiClient.get<CryptoCurrencyDetail>(`${this.baseEndpoint}/${id}`);
  }

  getPriceHistory(id: string, days: number): Observable<PriceHistoryPoint[]> {
    return this.apiClient.get<PriceHistoryPoint[]>(`${this.baseEndpoint}/${id}/history`, { days });
  }

  getMarketOverview(): Observable<MarketOverview> {
    return this.apiClient.get<MarketOverview>(`${this.baseEndpoint}/overview`);
  }
}