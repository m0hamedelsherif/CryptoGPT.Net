// Domain model for cryptocurrency
export interface CryptoCurrency {
  id: string;
  name: string;
  symbol: string;
  currentPrice: number;
  marketCap: number;
  changePercentage24h: number;
  rank: number;
}

// Detailed information about a cryptocurrency
export interface CryptoCurrencyDetail extends CryptoCurrency {
  description?: string;
  website?: string;
  circulatingSupply?: number;
  totalSupply?: number;
  allTimeHigh?: number;
  allTimeHighDate?: Date;
  technicalAnalysis?: TechnicalAnalysis;
}

// Technical analysis data
export interface TechnicalAnalysis {
  trend: string;
  signalStrength: number;
  supportLevels: number[];
  resistanceLevels: number[];
  indicators: {
    rsi?: number;
    macd?: {
      value: number;
      signal: number;
      histogram: number;
    };
    movingAverages?: {
      sma20: number;
      sma50: number;
      sma200: number;
    };
  };
}

// Price history data point
export interface PriceHistoryPoint {
  date: Date;
  price: number;
  volume: number;
}

// Market overview data
export interface MarketOverview {
  totalMarketCap: number;
  totalVolume24h: number;
  btcDominance: number;
  marketSentiment: string;
  topGainers: CryptoCurrency[];
  topLosers: CryptoCurrency[];
}

// News item related to cryptocurrencies
export interface CryptoNewsItem {
  id: string;
  title: string;
  summary: string;
  url: string;
  source: string;
  publishedAt: Date;
  sentiment?: string;
  relatedCoins?: string[];
}

// User risk profile
export interface RiskProfile {
  id: string;
  userId: string;
  riskTolerance: 'low' | 'medium' | 'high';
  investmentTimeframe: 'short' | 'medium' | 'long';
  investmentGoals: string[];
  preferredCryptocurrencies?: string[];
}