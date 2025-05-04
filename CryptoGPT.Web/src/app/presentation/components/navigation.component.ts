import { Component, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AppStateService } from '../../application/state/app-state.service';

@Component({
  selector: 'app-navigation',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <nav class="main-nav" [class.scrolled]="scrolled">
      <div class="container-fluid nav-container">
        <div class="logo-container">
          <div class="menu-toggle d-md-none" (click)="toggleMobileMenu()">
            <i class="bi" [ngClass]="{'bi-list': !mobileMenuOpen, 'bi-x': mobileMenuOpen}"></i>
          </div>
          <div class="logo" routerLink="/">
            <i class="bi bi-graph-up-arrow me-2"></i>
            CryptoGPT
          </div>
        </div>
        
        <div class="nav-links" [class.nav-open]="mobileMenuOpen">
          <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{exact: true}" (click)="closeMobileMenu()">
            <i class="bi bi-speedometer2 d-md-none me-2"></i>
            Dashboard
          </a>
          <a routerLink="/coins" routerLinkActive="active" (click)="closeMobileMenu()">
            <i class="bi bi-currency-bitcoin d-md-none me-2"></i>
            Market
          </a>
          <a routerLink="/news" routerLinkActive="active" (click)="closeMobileMenu()">
            <i class="bi bi-newspaper d-md-none me-2"></i>
            News
          </a>
          <a routerLink="/recommendations" routerLinkActive="active" (click)="closeMobileMenu()">
            <i class="bi bi-graph-up d-md-none me-2"></i>
            Recommendations
          </a>
        </div>
        
        <div class="user-actions">
          <div class="theme-toggle" (click)="toggleTheme()" aria-label="Toggle dark mode">
            <i class="bi" [ngClass]="isDarkMode ? 'bi-sun' : 'bi-moon'"></i>
          </div>
          <div class="profile-menu">
            <i class="bi bi-person-circle"></i>
          </div>
        </div>
      </div>
    </nav>
    <div class="nav-backdrop" *ngIf="mobileMenuOpen" (click)="closeMobileMenu()"></div>
  `,
  styles: [
    `
    .main-nav {
      background-color: var(--bg-card);
      color: var(--text-primary);
      padding: 0.75rem 0;
      box-shadow: var(--shadow-sm);
      position: sticky;
      top: 0;
      width: 100%;
      z-index: 1000;
      transition: all 0.3s ease;
    }
    
    .nav-container {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0 1.5rem;
    }
    
    .logo-container {
      display: flex;
      align-items: center;
    }
    
    .logo {
      font-size: 1.4rem;
      font-weight: 700;
      color: var(--primary-color);
      cursor: pointer;
      display: flex;
      align-items: center;
      transition: color 0.2s ease;
      
      i {
        font-size: 1.5rem;
      }
      
      &:hover {
        color: var(--primary-light);
      }
    }
    
    .nav-links {
      display: flex;
      gap: 2rem;
      margin: 0 2rem;
      
      a {
        color: var(--text-primary);
        text-decoration: none;
        font-weight: 500;
        padding: 0.5rem 0;
        position: relative;
        transition: color 0.2s ease;
        
        &:hover {
          color: var(--primary-color);
        }
        
        &:after {
          content: '';
          position: absolute;
          bottom: -2px;
          left: 0;
          width: 0;
          height: 2px;
          background-color: var(--primary-color);
          transition: width 0.3s ease;
        }
        
        &:hover:after,
        &.active:after {
          width: 100%;
        }
        
        &.active {
          color: var(--primary-color);
          font-weight: 600;
        }
      }
    }
    
    .user-actions {
      display: flex;
      align-items: center;
      gap: 1.5rem;
    }
    
    .theme-toggle {
      background: none;
      border: none;
      color: var(--text-primary);
      font-size: 1.2rem;
      cursor: pointer;
      width: 40px;
      height: 40px;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 50%;
      transition: all 0.2s ease;
      
      &:hover {
        background-color: rgba(0, 0, 0, 0.05);
        color: var(--primary-color);
      }
    }
    
    .profile-menu {
      font-size: 1.4rem;
      cursor: pointer;
      width: 40px;
      height: 40px;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 50%;
      transition: all 0.2s ease;
      
      &:hover {
        background-color: rgba(0, 0, 0, 0.05);
        color: var(--primary-color);
      }
    }
    
    .menu-toggle {
      display: none;
    }
    
    .scrolled {
      box-shadow: var(--shadow-md);
      padding: 0.5rem 0;
    }
    
    .nav-backdrop {
      display: none;
    }
    
    /* Mobile styles */
    @media (max-width: 767.98px) {
      .menu-toggle {
        display: flex;
        align-items: center;
        justify-content: center;
        cursor: pointer;
        font-size: 1.8rem;
        margin-right: 1rem;
      }
      
      .nav-links {
        position: fixed;
        top: 67px;
        left: 0;
        flex-direction: column;
        background-color: var(--bg-card);
        width: 250px;
        height: calc(100vh - 67px);
        padding: 1.5rem;
        box-shadow: var(--shadow-md);
        transform: translateX(-100%);
        transition: transform 0.3s ease;
        margin: 0;
        z-index: 1000;
        gap: 1.5rem;
        
        &.nav-open {
          transform: translateX(0);
        }
        
        a {
          display: flex;
          align-items: center;
          padding: 0.8rem 0;
          
          i {
            font-size: 1.2rem;
          }
        }
      }
      
      .nav-backdrop {
        display: block;
        position: fixed;
        top: 67px;
        left: 0;
        width: 100%;
        height: calc(100vh - 67px);
        background-color: rgba(0, 0, 0, 0.5);
        z-index: 999;
      }
    }
    
    /* Dark mode styles */
    :host-context(.dark-mode) {
      .main-nav {
        background-color: var(--bg-card-dark);
      }
      
      .theme-toggle:hover, 
      .profile-menu:hover {
        background-color: rgba(255, 255, 255, 0.05);
      }
      
      @media (max-width: 767.98px) {
        .nav-links {
          background-color: var(--bg-card-dark);
        }
      }
    }
    `
  ]
})
export class NavigationComponent implements OnInit {
  mobileMenuOpen = false;
  scrolled = false;
  isDarkMode = false;

  constructor(private appState: AppStateService) {
    // Initialize isDarkMode from the signal value
    this.isDarkMode = this.appState.darkMode();
  }
  
  ngOnInit(): void {
    // Watch for dark mode changes
    const darkModeObserver = new MutationObserver(() => {
      this.isDarkMode = this.appState.darkMode();
    });
    
    // Observe body class changes to detect theme toggle
    darkModeObserver.observe(document.body, {
      attributes: true,
      attributeFilter: ['class']
    });
  }
  
  @HostListener('window:scroll')
  onScroll() {
    if (window.scrollY > 10) {
      this.scrolled = true;
    } else {
      this.scrolled = false;
    }
  }
  
  toggleTheme(): void {
    this.appState.toggleDarkMode();
  }
  
  toggleMobileMenu(): void {
    this.mobileMenuOpen = !this.mobileMenuOpen;
    // Prevent body scrolling when menu is open
    document.body.style.overflow = this.mobileMenuOpen ? 'hidden' : '';
  }
  
  closeMobileMenu(): void {
    if (this.mobileMenuOpen) {
      this.mobileMenuOpen = false;
      document.body.style.overflow = '';
    }
  }
}