import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from './api-client.service';
import { CryptoNewsItem } from '../../domain/models/crypto-currency.model';

@Injectable({
  providedIn: 'root'
})
export class NewsApiService {
  private baseEndpoint = 'news';

  constructor(private apiClient: ApiClientService) {}

  getNews(count: number = 10): Observable<CryptoNewsItem[]> {
    return this.apiClient.get<CryptoNewsItem[]>(this.baseEndpoint, { count });
  }

  getNewsForCoin(coinId: string, count: number = 5): Observable<CryptoNewsItem[]> {
    return this.apiClient.get<CryptoNewsItem[]>(`${this.baseEndpoint}/coin/${coinId}`, { count });
  }
}