import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { RecommendationApiService } from '../../infrastructure/api/recommendation-api.service';
import { AppStateService } from '../state/app-state.service';
import { CryptoCurrency, RiskProfile } from '../../domain/models/crypto-currency.model';
import { CryptoAnalyticsService } from '../../domain/services/crypto-analytics.service';

@Injectable({
  providedIn: 'root'
})
export class RecommendationService {
  // Use the state observables from the AppStateService
  recommendations$ = this.appState.recommendations$;
  userProfile$ = this.appState.userProfile$;

  constructor(
    private recommendationApiService: RecommendationApiService,
    private cryptoAnalytics: CryptoAnalyticsService,
    private appState: AppStateService
  ) {}

  loadRecommendations(riskProfile: RiskProfile): Observable<CryptoCurrency[]> {
    this.setLoading(true);
    
    // Save the user profile in state
    this.appState.setUserProfile(riskProfile);
    
    return this.recommendationApiService.getRecommendations(riskProfile).pipe(
      tap(recommendations => {
        // Store the recommendations in state
        this.appState.setRecommendations(recommendations);
      }),
      catchError(error => {
        console.error('Error loading recommendations', error);
        this.setError('Failed to load personalized recommendations');
        return of([]);
      }),
      finalize(() => this.setLoading(false))
    );
  }

  saveUserProfile(profile: RiskProfile): Observable<RiskProfile> {
    return this.recommendationApiService.saveRiskProfile(profile).pipe(
      tap(savedProfile => {
        // Store the user profile in state
        this.appState.setUserProfile(savedProfile);
      })
    );
  }
  
  private setLoading(loading: boolean): void {
    this.updateRecommendationState({
      loading
    });
  }
  
  private setError(error: string | null): void {
    this.updateRecommendationState({
      error,
      loading: false
    });
  }
  
  private updateRecommendationState(update: Partial<{loading: boolean, error: string | null}>): void {
    this.appState.updateState({
      recommendations: {
        ...this.appState.currentState.recommendations,
        ...update
      }
    });
  }
}