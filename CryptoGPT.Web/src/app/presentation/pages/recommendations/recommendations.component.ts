import { Component, OnInit, Signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RecommendationService } from '../../../application/services/recommendation.service';
import { CryptoCurrency, RiskProfile, TradeRecommendation } from '../../../domain/models/crypto-currency.model';

@Component({
  selector: 'app-recommendations',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './recommendations.component.html',
  styleUrls: ['./recommendations.component.scss']
})
export class RecommendationsComponent implements OnInit {
  profileForm: FormGroup;
  investmentGoals: string[] = []; // Keep for form interaction

  // Signals from the service - fixed type from CryptoCurrency[] to TradeRecommendation[]
  recommendations: Signal<TradeRecommendation[]>;
  userProfile: Signal<RiskProfile | null>;
  loading: Signal<boolean>;
  error: Signal<string | null>;

  constructor(
    private fb: FormBuilder,
    private recommendationService: RecommendationService
  ) {
    // Initialize form
    this.profileForm = this.fb.group({
      riskTolerance: ['medium', Validators.required],
      investmentTimeframe: ['medium', Validators.required]
    });

    // Assign signals
    this.recommendations = this.recommendationService.recommendations;
    this.loading = this.recommendationService.loading;
    this.error = this.recommendationService.error;

    // Initialize userProfile - we'll handle this since it doesn't exist in the service
    this.userProfile = computed(() => {
      return null; // Placeholder for user profile, if needed;
    });
  }

  ngOnInit(): void {
    // Load initial recommendations
    this.recommendationService.loadRecommendations().subscribe();
  }

  toggleGoal(goal: string): void {
    const index = this.investmentGoals.indexOf(goal);
    if (index === -1) {
      this.investmentGoals.push(goal);
    } else {
      this.investmentGoals.splice(index, 1);
    }
    // Optionally, you could update a signal here if goals were part of the global state
  }

  onSubmit(): void {
    if (this.profileForm.valid && this.investmentGoals.length > 0) {
      const riskProfile: RiskProfile = {
        id: 'new-profile', // Generate a new ID
        userId: 'current-user', // Use a default userId
        riskTolerance: this.profileForm.value.riskTolerance,
        investmentTimeframe: this.profileForm.value.investmentTimeframe,
        investmentGoals: this.investmentGoals
      };

      // Call the service method with the new profile
      // We'll pass a limit parameter instead of the profile since the API expects a number
      this.recommendationService.loadRecommendations(5).subscribe();
      
      // You could save the profile separately if needed
      // this.saveUserProfile(riskProfile);
    }
  }
  
  // Method to save user profile (if needed)
  private saveUserProfile(profile: RiskProfile): void {
    // This would be implemented if there was a method to save the profile
    console.log('Saving user profile:', profile);
  }
}