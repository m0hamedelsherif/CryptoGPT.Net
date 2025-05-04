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

  getNews(limit: number = 10, page: number = 1): Observable<CryptoNewsItem[]> {
    return this.apiClient.get<CryptoNewsItem[]>(this.baseEndpoint, { limit, page });
  }

  getNewsForCoin(coinId: string, limit: number = 5): Observable<CryptoNewsItem[]> {
    return this.apiClient.get<CryptoNewsItem[]>(`${this.baseEndpoint}/${coinId}`, { limit });
  }
}