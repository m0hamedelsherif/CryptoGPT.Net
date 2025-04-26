import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class CryptoDataService {
  private apiUrl = 'http://localhost:5238/api'; // Default API URL

  constructor(private http: HttpClient) { }

  // Get top cryptocurrencies by market cap
  getTopCoins(limit: number = 10): Observable<any> {
    return this.http.get(`${this.apiUrl}/coin?limit=${limit}`);
  }

  // Get detailed information for a specific cryptocurrency
  getCoinDetails(coinId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/coin/${coinId}`);
  }

  // Get historical market data for a cryptocurrency
  getMarketChart(coinId: string, days: number = 30): Observable<any> {
    return this.http.get(`${this.apiUrl}/coin/${coinId}/chart?days=${days}`);
  }

  // Get overall market stats, including top gainers and losers
  getMarketOverview(): Observable<any> {
    return this.http.get(`${this.apiUrl}/coin/overview`);
  }

  // Get technical analysis for a specific cryptocurrency
  getTechnicalAnalysis(symbol: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/coin/${symbol}/technical-analysis`);
  }

  // Get available technical indicators
  getAvailableIndicators(): Observable<any> {
    return this.http.get(`${this.apiUrl}/coin/technical-indicators`);
  }

  // Get the current data source (e.g., CoinGecko)
  getDataSource(): Observable<any> {
    return this.http.get(`${this.apiUrl}/coin/source`);
  }
}
