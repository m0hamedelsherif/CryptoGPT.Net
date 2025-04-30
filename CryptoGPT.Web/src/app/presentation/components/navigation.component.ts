import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AppStateService } from '../../application/state/app-state.service';

@Component({
  selector: 'app-navigation',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <nav class="main-nav">
      <div class="logo" routerLink="/">CryptoGPT</div>
      
      <div class="nav-links">
        <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{exact: true}">Dashboard</a>
        <a routerLink="/coins" routerLinkActive="active">Market</a>
        <a routerLink="/news" routerLinkActive="active">News</a>
        <a routerLink="/recommendations" routerLinkActive="active">Recommendations</a>
      </div>
      
      <div class="user-actions">
        <button class="theme-toggle" (click)="toggleTheme()">
          <i class="fa fa-moon-o" aria-hidden="true"></i>
        </button>
      </div>
    </nav>
  `,
  styles: [
    `
    .main-nav {
      display: flex;
      justify-content: space-between;
      align-items: center;
      background-color: #2a2a2a;
      color: white;
      padding: 1rem 2rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }
    
    .logo {
      font-size: 1.5rem;
      font-weight: bold;
      cursor: pointer;
    }
    
    .nav-links {
      display: flex;
      gap: 2rem;
    }
    
    .nav-links a {
      color: white;
      text-decoration: none;
      font-weight: 500;
      padding: 0.5rem 0;
      position: relative;
    }
    
    .nav-links a:after {
      content: '';
      position: absolute;
      bottom: 0;
      left: 0;
      width: 0;
      height: 2px;
      background-color: #0099ff;
      transition: width 0.3s;
    }
    
    .nav-links a:hover:after,
    .nav-links a.active:after {
      width: 100%;
    }
    
    .user-actions {
      display: flex;
      gap: 1rem;
    }
    
    .theme-toggle {
      background: none;
      border: none;
      color: white;
      font-size: 1.2rem;
      cursor: pointer;
    }
    `
  ]
})
export class NavigationComponent {
  constructor(private appState: AppStateService) {}
  
  toggleTheme(): void {
    this.appState.toggleDarkMode();
  }
}