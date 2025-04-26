import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export enum RiskProfile {
  Conservative = 'Conservative',
  Moderate = 'Moderate',
  Aggressive = 'Aggressive'
}

@Injectable({
  providedIn: 'root'
})
export class RecommendationService {
  private apiUrl = 'http://localhost:5238/api';

  constructor(private http: HttpClient) { }

  // Generate investment recommendations based on query and risk profile
  generateRecommendations(query: string, riskProfile: RiskProfile = RiskProfile.Moderate): Observable<any> {
    return this.http.post(`${this.apiUrl}/recommendation`, {
      query: query,
      riskProfile: riskProfile
    });
  }

  // Get current market snapshot
  getMarketSnapshot(): Observable<any> {
    return this.http.get(`${this.apiUrl}/recommendation/market-snapshot`);
  }
}
