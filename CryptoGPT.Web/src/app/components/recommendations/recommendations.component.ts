import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RecommendationService, RiskProfile } from '../../services/recommendation.service';

@Component({
  selector: 'app-recommendations',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './recommendations.component.html',
  styleUrls: ['./recommendations.component.css']
})
export class RecommendationsComponent implements OnInit {
  recommendationForm: FormGroup;
  isLoading = false;
  recommendations: any = null;
  error: string | null = null;
  RiskProfile = RiskProfile; // Expose enum to template

  constructor(
    private fb: FormBuilder,
    private recommendationService: RecommendationService
  ) {
    this.recommendationForm = this.fb.group({
      query: ['', [Validators.required, Validators.minLength(10)]],
      riskProfile: [RiskProfile.Moderate, Validators.required]
    });
  }

  ngOnInit(): void {
  }

  generateRecommendations(): void {
    if (this.recommendationForm.invalid) {
      return;
    }

    this.isLoading = true;
    this.error = null;
    this.recommendations = null;

    const formValue = this.recommendationForm.value;

    this.recommendationService.generateRecommendations(formValue.query, formValue.riskProfile)
      .subscribe({
        next: (data) => {
          this.recommendations = data;
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Error generating recommendations:', err);
          this.error = 'Failed to generate recommendations. Please try again later.';
          this.isLoading = false;
        }
      });
  }
}
