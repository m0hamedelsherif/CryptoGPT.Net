import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { signal, computed } from '@angular/core';
import { TradeRecommendation } from '../../domain/models/crypto-currency.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class RecommendationService {
  private apiUrl = environment.apiUrl;
  
  // State signals
  private recommendationsSignal = signal<TradeRecommendation[]>([]);
  private loadingSignal = signal<boolean>(false);
  private errorSignal = signal<string | null>(null);
  
  // Public computed signals
  readonly recommendations = computed(() => this.recommendationsSignal());
  readonly loading = computed(() => this.loadingSignal());
  readonly error = computed(() => this.errorSignal());
  
  constructor(private http: HttpClient) {}
  
  /**
   * Load AI trading recommendations
   * @param limit Optional limit parameter
   * @returns Observable that completes when data is loaded
   */
  loadRecommendations(limit: number = 5): Observable<TradeRecommendation[]> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);
    
    return this.http.get<any>(`${this.apiUrl}/recommendation?limit=${limit}`)
      .pipe(
        map(response => this.mapRecommendations(response)),
        tap(recommendations => {
          this.recommendationsSignal.set(recommendations);
          this.loadingSignal.set(false);
        }),
        catchError(error => {
          console.error('Error loading recommendations:', error);
          this.errorSignal.set(error.message || 'Failed to load recommendations');
          this.loadingSignal.set(false);
          return of([] as TradeRecommendation[]);
        })
      );
  }
  
  /**
   * Load recommendations for a specific coin
   * @param coinId Coin identifier
   * @returns Observable with recommendations for the specified coin
   */
  loadCoinRecommendations(coinId: string): Observable<TradeRecommendation[]> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);
    
    return this.http.get<any>(`${this.apiUrl}/recommendation/${coinId}`)
      .pipe(
        map(response => this.mapRecommendations(response)),
        tap(recommendations => {
          this.recommendationsSignal.set(recommendations);
          this.loadingSignal.set(false);
        }),
        catchError(error => {
          console.error(`Error loading recommendations for ${coinId}:`, error);
          this.errorSignal.set(error.message || 'Failed to load recommendations');
          this.loadingSignal.set(false);
          return of([] as TradeRecommendation[]);
        })
      );
  }
  
  /**
   * Map API response to TradeRecommendation objects
   * @param response Response from API
   * @returns Array of TradeRecommendation objects
   */
  private mapRecommendations(response: any): TradeRecommendation[] {
    if (!response || !response.recommendations) {
      return [];
    }
    
    return response.recommendations.map((rec: any) => ({
      id: rec.id,
      coinId: rec.coinId,
      coinName: rec.coinName,
      coinSymbol: rec.coinSymbol,
      type: rec.type,
      price: rec.price,
      targetPrice: rec.targetPrice,
      stopLoss: rec.stopLoss,
      confidence: rec.confidence || 'medium',
      reasoning: rec.reasoning,
      timestamp: new Date(rec.timestamp),
      expiryDate: rec.expiryDate ? new Date(rec.expiryDate) : undefined
    }));
  }
}