import { Component, OnInit, Signal, WritableSignal, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CryptoService } from '../../../application/services/crypto.service';
import { CryptoCurrency } from '../../../domain/models/crypto-currency.model';
import { FormatLargeNumberPipe } from '../../../shared/pipes/format-large-number.pipe';

@Component({
  selector: 'app-crypto-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormatLargeNumberPipe],
  templateUrl: './crypto-list.component.html',
  styleUrls: ['./crypto-list.component.scss']
})
export class CryptoListComponent implements OnInit {
  private cryptoService = inject(CryptoService);

  // Core data signals
  allCoins: Signal<CryptoCurrency[]> = this.cryptoService.coins;
  isLoading: Signal<boolean> = this.cryptoService.loading;
  error: Signal<string | null> = this.cryptoService.error;

  // Filter and sort signals
  searchTerm: WritableSignal<string> = signal('');
  sortBy: WritableSignal<keyof CryptoCurrency | 'marketCapRank'> = signal('marketCapRank');
  sortDirection: WritableSignal<'asc' | 'desc'> = signal('desc');

  // Pagination signals
  currentPage: WritableSignal<number> = signal(1);
  itemsPerPage: number = 10;

  // Computed signals for filtered and paginated data
  filteredCoins: Signal<CryptoCurrency[]> = computed(() => {
    const term = this.searchTerm().toLowerCase();
    const coins = this.allCoins();
    const sortKey = this.sortBy();
    const direction = this.sortDirection();

    let filtered = coins;
    if (term) {
      filtered = coins.filter(coin =>
        coin.name.toLowerCase().includes(term) ||
        coin.symbol.toLowerCase().includes(term)
      );
    }

    return [...filtered].sort((a, b) => {
      const valA = a[sortKey as keyof CryptoCurrency] ?? (sortKey === 'marketCapRank' ? Infinity : (sortKey === 'priceChangePercentage24h' ? 0 : ''));
      const valB = b[sortKey as keyof CryptoCurrency] ?? (sortKey === 'marketCapRank' ? Infinity : (sortKey === 'priceChangePercentage24h' ? 0 : ''));

      // Handle string comparisons
      if (typeof valA === 'string' && typeof valB === 'string') {
        return direction === 'asc' 
          ? valA.localeCompare(valB) 
          : valB.localeCompare(valA);
      }

      // Handle numeric comparisons
      let comparison = 0;
      if (valA > valB) {
        comparison = 1;
      } else if (valA < valB) {
        comparison = -1;
      }
      
      return direction === 'asc' ? comparison : comparison * -1;
    });
  });

  // Total pages for pagination
  totalPages = computed(() => {
    return Math.max(1, Math.ceil(this.filteredCoins().length / this.itemsPerPage));
  });

  // Current page data
  paginatedCoins = computed(() => {
    const startIndex = (this.currentPage() - 1) * this.itemsPerPage;
    return this.filteredCoins().slice(startIndex, startIndex + this.itemsPerPage);
  });

  // Page number array for pagination UI
  pageNumbers = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const maxVisiblePages = 5;
    
    if (total <= maxVisiblePages) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }
    
    // Always show first, last, and pages around current
    const pages = [];
    if (current <= 3) {
      // Near beginning
      for (let i = 1; i <= 5; i++) pages.push(i);
    } else if (current >= total - 2) {
      // Near end
      for (let i = total - 4; i <= total; i++) pages.push(i);
    } else {
      // Middle
      for (let i = current - 2; i <= current + 2; i++) pages.push(i);
    }
    
    return pages;
  });

  constructor() {}

  ngOnInit(): void {
    this.loadData();
  }

  // Load or refresh data
  loadData(): void {
    this.cryptoService.loadCoins().subscribe({
      error: (err) => console.error('Error loading cryptocurrencies:', err)
    });
  }
  
  // Refresh data manually
  refreshData(): void {
    if (!this.isLoading()) {
      this.loadData();
    }
  }

  // Search handling
  onSearch(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
    this.currentPage.set(1); // Reset to first page on search
  }

  // Sort handling
  onSortChange(event: Event): void {
    this.sortBy.set((event.target as HTMLSelectElement).value as keyof CryptoCurrency | 'marketCapRank');
  }

  // Sort direction handling
  onSortDirectionChange(event: Event): void {
    this.sortDirection.set((event.target as HTMLSelectElement).value as 'asc' | 'desc');
  }

  setSortDirection(direction: 'asc' | 'desc'): void {
    this.sortDirection.set(direction);
  }

  setSortBy(key: keyof CryptoCurrency | 'marketCapRank'): void {
    if (this.sortBy() === key) {
      this.sortDirection.update(dir => dir === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(key);
    }
  }

  // Reset all filters
  resetFilters(): void {
    this.searchTerm.set('');
    this.sortBy.set('marketCapRank');
    this.sortDirection.set('desc');
    this.currentPage.set(1);
  }

  // Pagination navigation
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.currentPage()) {
      return;
    }
    this.currentPage.set(page);
    // Scroll to top of the table when changing pages
    const tableElement = document.querySelector('.crypto-table') as HTMLElement;
    window.scrollTo({ top: tableElement?.offsetTop || 0, behavior: 'smooth' });
  }

  // Format large numbers for display
  formatLargeNumber(value: number | undefined): string {
    if (value === undefined || value === null) {
      return 'N/A';
    }
    
    if (value >= 1e12) {
      return (value / 1e12).toFixed(2) + 'T';
    }
    if (value >= 1e9) {
      return (value / 1e9).toFixed(2) + 'B';
    }
    if (value >= 1e6) {
      return (value / 1e6).toFixed(2) + 'M';
    }
    if (value >= 1e3) {
      return (value / 1e3).toFixed(2) + 'K';
    }
    return value.toFixed(2);
  }
}