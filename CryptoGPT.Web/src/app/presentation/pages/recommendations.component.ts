import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RecommendationService } from '../../../application/services/recommendation.service';
import { CryptoCurrency, RiskProfile } from '../../../domain/models/crypto-currency.model';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-recommendations',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  template: `
    <div class="recommendations-page">
      <h1>Personalized Crypto Recommendations</h1>
      
      <div class="risk-profile-form">
        <h2>Your Investment Profile</h2>
        <form [formGroup]="profileForm" (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label for="riskTolerance">Risk Tolerance</label>
            <select id="riskTolerance" formControlName="riskTolerance">
              <option value="low">Low - I prefer safer investments</option>
              <option value="medium">Medium - I can accept moderate risk</option>
              <option value="high">High - I'm comfortable with significant risk</option>
            </select>
          </div>
          
          <div class="form-group">
            <label for="investmentTimeframe">Investment Timeframe</label>
            <select id="investmentTimeframe" formControlName="investmentTimeframe">
              <option value="short">Short - Less than 1 year</option>
              <option value="medium">Medium - 1-3 years</option>
              <option value="long">Long - 3+ years</option>
            </select>
          </div>
          
          <div class="form-group">
            <label>Investment Goals</label>
            <div class="checkbox-group">
              <label>
                <input type="checkbox" (change)="toggleGoal('growth')"> Growth
              </label>
              <label>
                <input type="checkbox" (change)="toggleGoal('income')"> Income
              </label>
              <label>
                <input type="checkbox" (change)="toggleGoal('preservation')"> Capital Preservation
              </label>
              <label>
                <input type="checkbox" (change)="toggleGoal('speculation')"> Speculation
              </label>
            </div>
          </div>
          
          <button type="submit" [disabled]="!profileForm.valid">Get Recommendations</button>
        </form>
      </div>
      
      <div class="recommendations-list" *ngIf="recommendations.length > 0">
        <h2>Your Personalized Recommendations</h2>
        <div class="recommendation-card" *ngFor="let coin of recommendations" [routerLink]="['/coin', coin.id]">
          <div class="rank">{{ coin.rank }}</div>
          <div class="name-symbol">
            <span class="name">{{ coin.name }}</span>
            <span class="symbol">{{ coin.symbol }}</span>
          </div>
          <div class="price">${{ coin.currentPrice | number:'1.2-6' }}</div>
          <div class="change" 
               [class.positive]="coin.changePercentage24h > 0"
               [class.negative]="coin.changePercentage24h < 0">
            {{ coin.changePercentage24h | number:'1.2-2' }}%
          </div>
          <div class="market-cap">${{ coin.marketCap | number }}</div>
        </div>
      </div>
      
      <div class="no-recommendations" *ngIf="formSubmitted && recommendations.length === 0">
        <p>We're generating your personalized recommendations. Please wait a moment...</p>
      </div>
      
      <div class="recommendation-explanation" *ngIf="recommendations.length > 0">
        <h3>Why We Recommend These Assets</h3>
        <p>
          Based on your {{ profileForm.value.riskTolerance }} risk tolerance and 
          {{ profileForm.value.investmentTimeframe }} investment timeframe, we've selected cryptocurrencies 
          that align with your financial goals. This personalized portfolio aims to balance potential returns 
          with your comfort level for market volatility.
        </p>
        
        <div class="disclaimer">
          <p><strong>Disclaimer:</strong> These recommendations are provided for informational purposes only and 
          do not constitute investment advice. Always conduct your own research before making investment decisions.</p>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
    .recommendations-page {
      padding: 1rem;
      max-width: 1200px;
      margin: 0 auto;
    }
    
    .risk-profile-form {
      background-color: #f5f5f5;
      border-radius: 8px;
      padding: 1.5rem;
      margin-bottom: 2rem;
    }
    
    .form-group {
      margin-bottom: 1rem;
    }
    
    label {
      display: block;
      margin-bottom: 0.5rem;
      font-weight: bold;
    }
    
    select, input {
      width: 100%;
      padding: 0.5rem;
      border: 1px solid #ccc;
      border-radius: 4px;
    }
    
    .checkbox-group {
      display: flex;
      flex-wrap: wrap;
      gap: 1rem;
    }
    
    .checkbox-group label {
      display: flex;
      align-items: center;
      font-weight: normal;
    }
    
    .checkbox-group input {
      width: auto;
      margin-right: 0.5rem;
    }
    
    button {
      background-color: #0066cc;
      color: white;
      padding: 0.75rem 1.5rem;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: bold;
    }
    
    button:disabled {
      background-color: #cccccc;
    }
    
    .recommendation-card {
      display: grid;
      grid-template-columns: 0.5fr 2fr 1fr 1fr 1.5fr;
      padding: 1rem;
      border-bottom: 1px solid #eee;
      align-items: center;
      cursor: pointer;
    }
    
    .recommendation-card:hover {
      background-color: #f9f9f9;
    }
    
    .rank {
      font-weight: bold;
    }
    
    .name-symbol {
      display: flex;
      flex-direction: column;
    }
    
    .symbol {
      color: #666;
      font-size: 0.9rem;
    }
    
    .price {
      font-weight: bold;
    }
    
    .positive {
      color: green;
    }
    
    .negative {
      color: red;
    }
    
    .recommendation-explanation {
      margin-top: 2rem;
      background-color: #f0f7ff;
      border-radius: 8px;
      padding: 1.5rem;
    }
    
    .disclaimer {
      margin-top: 1.5rem;
      font-size: 0.9rem;
      color: #666;
      border-top: 1px solid #ddd;
      padding-top: 1rem;
    }
    `
  ]
})
export class RecommendationsComponent implements OnInit {
  profileForm: FormGroup;
  recommendations: CryptoCurrency[] = [];
  investmentGoals: string[] = [];
  formSubmitted = false;
  
  constructor(
    private fb: FormBuilder,
    private recommendationService: RecommendationService
  ) {
    this.profileForm = this.fb.group({
      riskTolerance: ['medium', Validators.required],
      investmentTimeframe: ['medium', Validators.required]
    });
  }
  
  ngOnInit(): void {
    // Potentially load a saved user profile if it exists
  }
  
  toggleGoal(goal: string): void {
    const index = this.investmentGoals.indexOf(goal);
    if (index === -1) {
      this.investmentGoals.push(goal);
    } else {
      this.investmentGoals.splice(index, 1);
    }
  }
  
  onSubmit(): void {
    if (this.profileForm.valid && this.investmentGoals.length > 0) {
      this.formSubmitted = true;
      
      const riskProfile: RiskProfile = {
        id: '', // Will be assigned by server
        userId: 'current-user', // In a real app, would use auth service
        riskTolerance: this.profileForm.value.riskTolerance,
        investmentTimeframe: this.profileForm.value.investmentTimeframe,
        investmentGoals: this.investmentGoals
      };
      
      this.recommendationService.loadRecommendations(riskProfile)
        .subscribe(recommendations => {
          this.recommendations = recommendations;
        });
    }
  }
}