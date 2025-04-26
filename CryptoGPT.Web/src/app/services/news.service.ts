import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class NewsService {
  private apiUrl = 'http://localhost:5238/api';

  constructor(private http: HttpClient) { }

  // Get latest crypto market news
  getMarketNews(limit: number = 20): Observable<any> {
    return this.http.get(`${this.apiUrl}/news?limit=${limit}`);
  }

  // Get news for a specific cryptocurrency
  getCoinNews(coinId: string, symbol: string, limit: number = 10): Observable<any> {
    return this.http.get(`${this.apiUrl}/news/${coinId}?symbol=${symbol}&limit=${limit}`);
  }
}
