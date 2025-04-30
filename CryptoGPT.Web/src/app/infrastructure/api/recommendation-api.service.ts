import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from './api-client.service';
import { CryptoCurrency, RiskProfile } from '../../domain/models/crypto-currency.model';

@Injectable({
  providedIn: 'root'
})
export class RecommendationApiService {
  private baseEndpoint = 'recommendation';

  constructor(private apiClient: ApiClientService) {}

  getRecommendations(riskProfile: RiskProfile): Observable<CryptoCurrency[]> {
    return this.apiClient.post<CryptoCurrency[]>(this.baseEndpoint, riskProfile);
  }

  getRiskProfiles(): Observable<RiskProfile[]> {
    return this.apiClient.get<RiskProfile[]>('profiles');
  }

  saveRiskProfile(profile: RiskProfile): Observable<RiskProfile> {
    return this.apiClient.post<RiskProfile>('profiles', profile);
  }
}