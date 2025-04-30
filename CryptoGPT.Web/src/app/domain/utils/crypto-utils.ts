import { CryptoCurrency } from '../models/crypto-currency.model';

/**
 * Pure domain utility functions for cryptocurrency data
 * These functions are independent of any infrastructure or application dependencies
 */

/**
 * Format price with appropriate precision based on value
 * @param price The price value to format
 * @returns Formatted price string
 */
export function formatPrice(price: number): string {
  if (price === undefined || price === null) {
    return '$0.00';
  }
  
  // For very small prices, show more decimal places
  if (price < 0.01) {
    return '$' + price.toFixed(6);
  } else if (price < 1) {
    return '$' + price.toFixed(4);
  } else if (price < 10000) {
    return '$' + price.toFixed(2);
  } else {
    // For large prices, use comma as thousands separator
    return '$' + price.toLocaleString('en-US', { 
      minimumFractionDigits: 2, 
      maximumFractionDigits: 2 
    });
  }
}

/**
 * Format market cap with appropriate abbreviation
 * @param marketCap The market cap value to format
 * @returns Formatted market cap string
 */
export function formatMarketCap(marketCap: number): string {
  if (!marketCap) return '$0';
  
  if (marketCap >= 1_000_000_000_000) {
    return '$' + (marketCap / 1_000_000_000_000).toFixed(2) + 'T';
  } else if (marketCap >= 1_000_000_000) {
    return '$' + (marketCap / 1_000_000_000).toFixed(2) + 'B';
  } else if (marketCap >= 1_000_000) {
    return '$' + (marketCap / 1_000_000).toFixed(2) + 'M';
  } else {
    return '$' + marketCap.toLocaleString('en-US');
  }
}

/**
 * Format percentage change with sign and appropriate color class
 * @param percentage The percentage change
 * @returns Object with formatted percentage and CSS class
 */
export function formatPercentageChange(percentage: number): { value: string, cssClass: string } {
  if (percentage === undefined || percentage === null) {
    return { value: '0.00%', cssClass: 'neutral' };
  }
  
  const formatted = percentage.toFixed(2) + '%';
  
  if (percentage > 0) {
    return { value: '+' + formatted, cssClass: 'positive' };
  } else if (percentage < 0) {
    return { value: formatted, cssClass: 'negative' };
  } else {
    return { value: formatted, cssClass: 'neutral' };
  }
}

/**
 * Sort cryptocurrencies by specified criteria
 * @param coins Array of cryptocurrencies to sort
 * @param sortBy Sorting criteria
 * @param sortDirection Direction to sort ('asc' or 'desc')
 * @returns Sorted array of cryptocurrencies
 */
export function sortCryptoCurrencies(
  coins: CryptoCurrency[], 
  sortBy: 'rank' | 'name' | 'price' | 'marketCap' | 'change' = 'rank',
  sortDirection: 'asc' | 'desc' = 'asc'
): CryptoCurrency[] {
  // Create a new array to avoid mutating the original
  const sortedCoins = [...coins];
  
  sortedCoins.sort((a, b) => {
    let comparison = 0;
    
    switch (sortBy) {
      case 'rank':
        comparison = a.rank - b.rank;
        break;
      case 'name':
        comparison = a.name.localeCompare(b.name);
        break;
      case 'price':
        comparison = a.currentPrice - b.currentPrice;
        break;
      case 'marketCap':
        comparison = a.marketCap - b.marketCap;
        break;
      case 'change':
        comparison = a.changePercentage24h - b.changePercentage24h;
        break;
      default:
        comparison = a.rank - b.rank;
    }
    
    return sortDirection === 'asc' ? comparison : -comparison;
  });
  
  return sortedCoins;
}

/**
 * Filter cryptocurrencies by search term
 * @param coins Array of cryptocurrencies to filter
 * @param searchTerm Term to search for in name or symbol
 * @returns Filtered array of cryptocurrencies
 */
export function filterCryptoCurrencies(coins: CryptoCurrency[], searchTerm: string): CryptoCurrency[] {
  if (!searchTerm || searchTerm.trim() === '') {
    return coins;
  }
  
  const term = searchTerm.toLowerCase().trim();
  
  return coins.filter(coin => 
    coin.name.toLowerCase().includes(term) || 
    coin.symbol.toLowerCase().includes(term)
  );
}

/**
 * Group cryptocurrencies by market segment
 * @param coins Array of cryptocurrencies
 * @returns Object with coins grouped by segment
 */
export function groupCoinsBySegment(coins: CryptoCurrency[]): Record<string, CryptoCurrency[]> {
  // This is a simplified implementation since we don't have segment data in our model
  // In a real app, you would have segment data in your model
  const segments: Record<string, CryptoCurrency[]> = {
    'Layer 1': [],
    'DeFi': [],
    'Exchange Tokens': [],
    'Privacy Coins': [],
    'Meme Coins': [],
    'Other': []
  };
  
  // Simplified logic to categorize coins - in a real app, this would be based on actual coin metadata
  coins.forEach(coin => {
    const symbol = coin.symbol.toLowerCase();
    
    if (['btc', 'eth', 'sol', 'ada', 'avax'].includes(symbol)) {
      segments['Layer 1'].push(coin);
    } else if (['uni', 'aave', 'comp', 'sushi', 'cake'].includes(symbol)) {
      segments['DeFi'].push(coin);
    } else if (['bnb', 'okb', 'cro', 'ftm', 'kcs'].includes(symbol)) {
      segments['Exchange Tokens'].push(coin);
    } else if (['xmr', 'dash', 'zec'].includes(symbol)) {
      segments['Privacy Coins'].push(coin);
    } else if (['doge', 'shib', 'elon'].includes(symbol)) {
      segments['Meme Coins'].push(coin);
    } else {
      segments['Other'].push(coin);
    }
  });
  
  // Remove empty segments
  Object.keys(segments).forEach(key => {
    if (segments[key].length === 0) {
      delete segments[key];
    }
  });
  
  return segments;
}