import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-error-message',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="error-container" *ngIf="message">
      <div class="error-icon">
        <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="12" cy="12" r="10"></circle>
          <line x1="12" y1="8" x2="12" y2="12"></line>
          <line x1="12" y1="16" x2="12.01" y2="16"></line>
        </svg>
      </div>
      <div class="error-content">
        <div class="error-title">{{ title }}</div>
        <div class="error-message">{{ message }}</div>
      </div>
      <button *ngIf="retryable" class="retry-button" (click)="onRetry.emit()">
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <path d="M21.5 2v6h-6M2.5 22v-6h6M2 11.5a10 10 0 0 1 18.8-4.3M22 12.5a10 10 0 0 1-18.8 4.2"/>
        </svg>
        Retry
      </button>
      <button class="close-button" (click)="onClose.emit()">
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <line x1="18" y1="6" x2="6" y2="18"></line>
          <line x1="6" y1="6" x2="18" y2="18"></line>
        </svg>
      </button>
    </div>
  `,
  styles: [`
    .error-container {
      display: flex;
      align-items: flex-start;
      padding: 16px;
      background-color: #fff0f0;
      border-left: 4px solid #ff3333;
      border-radius: 4px;
      margin: 12px 0;
      position: relative;
    }
    
    .error-icon {
      color: #ff3333;
      margin-right: 16px;
      flex-shrink: 0;
    }
    
    .error-content {
      flex-grow: 1;
    }
    
    .error-title {
      font-weight: bold;
      margin-bottom: 4px;
    }
    
    .error-message {
      color: #555;
    }
    
    .retry-button {
      background-color: transparent;
      border: 1px solid #ff3333;
      color: #ff3333;
      padding: 5px 10px;
      border-radius: 4px;
      cursor: pointer;
      margin-right: 8px;
      display: flex;
      align-items: center;
      gap: 5px;
    }
    
    .retry-button:hover {
      background-color: rgba(255, 51, 51, 0.1);
    }
    
    .close-button {
      background: transparent;
      border: none;
      color: #999;
      cursor: pointer;
      padding: 4px;
      position: absolute;
      top: 8px;
      right: 8px;
    }
    
    .close-button:hover {
      color: #333;
    }
    
    :host-context(.dark-mode) .error-container {
      background-color: rgba(255, 0, 0, 0.1);
      border-left-color: #ff5555;
    }
    
    :host-context(.dark-mode) .error-message {
      color: #ccc;
    }
  `]
})
export class ErrorMessageComponent {
  @Input() message: string = '';
  @Input() title: string = 'Error';
  @Input() retryable: boolean = false;
  
  @Output() onRetry = new EventEmitter<void>();
  @Output() onClose = new EventEmitter<void>();
}