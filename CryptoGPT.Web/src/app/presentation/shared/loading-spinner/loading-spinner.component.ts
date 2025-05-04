import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="spinner-container" [class.overlay]="overlay">
      <div class="spinner">
        <div class="bounce1"></div>
        <div class="bounce2"></div>
        <div class="bounce3"></div>
      </div>
      <div class="spinner-text" *ngIf="message">{{ message }}</div>
    </div>
  `,
  styles: [`
    .spinner-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 20px;
    }
    
    .spinner-container.overlay {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background-color: rgba(255, 255, 255, 0.7);
      z-index: 1000;
    }
    
    .spinner {
      width: 70px;
      text-align: center;
    }
    
    .spinner > div {
      width: 18px;
      height: 18px;
      background-color: #0066cc;
      border-radius: 100%;
      display: inline-block;
      animation: sk-bouncedelay 1.4s infinite ease-in-out both;
    }
    
    .spinner .bounce1 {
      animation-delay: -0.32s;
    }
    
    .spinner .bounce2 {
      animation-delay: -0.16s;
    }
    
    .spinner-text {
      margin-top: 15px;
      color: #555;
    }
    
    @keyframes sk-bouncedelay {
      0%, 80%, 100% { 
        transform: scale(0);
      } 40% { 
        transform: scale(1.0);
      }
    }
    
    :host-context(.dark-mode) .spinner-container.overlay {
      background-color: rgba(0, 0, 0, 0.7);
    }
    
    :host-context(.dark-mode) .spinner-text {
      color: #ccc;
    }
  `]
})
export class LoadingSpinnerComponent {
  @Input() overlay = false;
  @Input() message?: string;
}