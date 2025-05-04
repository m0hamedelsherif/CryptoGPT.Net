import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class NewsService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  // Get latest crypto news
  getLatestNews(limit: number = 10): Observable<any> {
    return this.http.get(`${this.apiUrl}/news?limit=${limit}`);
  }

  // Get news related to a specific cryptocurrency
  getCoinNews(coinId: string, symbol: string, limit: number = 5): Observable<any> {
    return this.http.get(`${this.apiUrl}/news/coin/${coinId}?symbol=${symbol}&limit=${limit}`);
  }

  // Get trending news topics in crypto
  getTrendingTopics(): Observable<any> {
    return this.http.get(`${this.apiUrl}/news/trending`);
  }

  // Search news by keyword
  searchNews(query: string, limit: number = 10): Observable<any> {
    return this.http.get(`${this.apiUrl}/news/search?query=${encodeURIComponent(query)}&limit=${limit}`);
  }
}
