import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CryptoService } from '../../../application/services/crypto.service';
import { CryptoCurrency } from '../../../domain/models/crypto-currency.model';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-crypto-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="crypto-list">
      <h2>Cryptocurrencies</h2>
      <div class="filter-bar">
        <input type="text" placeholder="Search coins..." (input)="onSearch($event)">
        <select (change)="onSort($event)">
          <option value="rank">Rank</option>
          <option value="name">Name</option>
          <option value="price">Price</option>
          <option value="change">24h Change</option>
        </select>
      </div>
      <div class="crypto-table">
        <div class="header">
          <div>Rank</div>
          <div>Name</div>
          <div>Price</div>
          <div>24h Change</div>
          <div>Market Cap</div>
        </div>
        <div class="coin-row" *ngFor="let coin of coins$ | async" [routerLink]="['/coin', coin.id]">
          <div>{{ coin.rank }}</div>
          <div>
            <span class="symbol">{{ coin.symbol }}</span>
            {{ coin.name }}
          </div>
          <div>${{ coin.currentPrice | number:'1.2-6' }}</div>
          <div [class.positive]="coin.changePercentage24h > 0" 
               [class.negative]="coin.changePercentage24h < 0">
            {{ coin.changePercentage24h | number:'1.2-2' }}%
          </div>
          <div>${{ coin.marketCap | number }}</div>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
    .crypto-list {
      padding: 1rem;
    }
    .filter-bar {
      display: flex;
      gap: 1rem;
      margin-bottom: 1rem;
    }
    .crypto-table .header {
      font-weight: bold;
      border-bottom: 1px solid #ccc;
    }
    .crypto-table .header, .crypto-table .coin-row {
      display: grid;
      grid-template-columns: 0.5fr 2fr 1fr 1fr 1.5fr;
      padding: 0.5rem 0;
      align-items: center;
    }
    .coin-row {
      cursor: pointer;
      border-bottom: 1px solid #eee;
    }
    .coin-row:hover {
      background-color: #f9f9f9;
    }
    .symbol {
      font-weight: bold;
      margin-right: 0.5rem;
    }
    .positive {
      color: green;
    }
    .negative {
      color: red;
    }
    `
  ]
})
export class CryptoListComponent implements OnInit {
  coins$: Observable<CryptoCurrency[]>;
  
  constructor(private cryptoService: CryptoService) {
    this.coins$ = this.cryptoService.coins$;
  }
  
  ngOnInit(): void {
    this.cryptoService.loadCoins().subscribe();
  }
  
  onSearch(event: Event): void {
    const searchTerm = (event.target as HTMLInputElement).value.toLowerCase();
    // Implementation for search filtering
  }
  
  onSort(event: Event): void {
    const sortBy = (event.target as HTMLSelectElement).value;
    // Implementation for sorting
  }
}