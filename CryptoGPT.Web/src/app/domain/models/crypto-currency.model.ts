// Basic cryptocurrency information - Aligned with CryptoCurrencyDto
export interface CryptoCurrency {
  id: string;
  symbol: string; // Keep symbol
  name: string;
  currentPrice: number;
  marketCap: number;
  priceChangePercentage24h?: number; // Align with backend nullability
  marketCapRank?: number; // Rename rank to marketCapRank, align nullability
  volume24h: number; // Add volume24h
  imageUrl: string; // Add imageUrl
  image?: string; // For compatibility with external APIs that use 'image' instead of 'imageUrl'
}

// Detailed cryptocurrency information - Aligned with CryptoCurrencyDetailDto
export interface CryptoCurrencyDetail { // Removed 'extends CryptoCurrency' as backend DTO repeats fields
  id: string;
  symbol: string;
  name: string;
  description?: string | null; // Ensure nullability
  imageUrl: string; // Add imageUrl (repeated from basic, align with backend)
  homepage?: string | null; // Rename website to homepage, ensure nullability
  whitepaper?: string | null; // Add whitepaper
  blockchainSite?: string | null; // Add blockchainSite
  twitter?: string | null; // Add twitter
  facebook?: string | null; // Add facebook
  subreddit?: string | null; // Add subreddit
  categories?: string[] | null; // Add categories
  hashingAlgorithm?: string | null; // Add hashingAlgorithm
  genesisDate?: string | null; // Add genesisDate (string for simplicity, convert if needed)
  sentimentVotesUpPercentage?: number | null; // Add sentimentVotesUpPercentage
  sentimentVotesDownPercentage?: number | null; // Add sentimentVotesDownPercentage
  currentPrice: number; // Add currentPrice (repeated from basic)
  marketCap: number; // Add marketCap (repeated from basic)
  priceChangePercentage24h?: number | null; // Add priceChangePercentage24h (repeated from basic)
  marketCapRank?: number | null; // Add marketCapRank (repeated from basic)
  volume24h: number; // Add volume24h (repeated from basic)
  circulatingSupply?: number | null; // Ensure nullability
  totalSupply?: number | null; // Ensure nullability
  maxSupply?: number | null; // Add maxSupply
  allTimeHigh?: number | null; // Ensure nullability
  allTimeHighDate?: string | null; // Use string for date, ensure nullability
  allTimeLow?: number | null; // Add allTimeLow
  allTimeLowDate?: string | null; // Add allTimeLowDate (string)
  high24h?: number | null; // Add high24h
  low24h?: number | null; // Add low24h
  // technicalAnalysis?: TechnicalAnalysis; // Keep or remove based on whether it's derived frontend-only
}

// Price history data point - Aligned with PriceHistoryPointDto
export interface PriceHistoryPoint {
  timestamp: number; // Rename date to timestamp, use number (Unix epoch ms)
  price: number;
  volume?: number | null; // Add optional volume
}

// Indicator time point - Aligned with IndicatorTimePointDto
export interface IndicatorTimePoint {
  timestamp: number; // Use number (Unix epoch ms)
  value: number;
}

// Market history data - Aligned with MarketHistoryDto
export interface MarketHistory {
  coinId: string;
  symbol: string;
  prices: PriceHistoryPoint[];
  marketCaps: PriceHistoryPoint[]; // Reusing PriceHistoryPoint for simplicity, backend uses separate fields
  volumes: PriceHistoryPoint[]; // Reusing PriceHistoryPoint for simplicity
  indicatorSeries?: Record<string, IndicatorTimePoint[]> | null; // Dictionary mapping indicator name to its time series data
}

// Market overview data - matches CryptoGPT.Core.Models.MarketOverview
export interface MarketOverview {
  totalMarketCap: number;
  totalVolume24h: number;
  btcDominance: number;
  marketSentiment: string;
  marketCapChangePercentage24h: number;
  volumeChangePercentage24h: number;
  btcDominanceChange: number;
  topPerformers: CryptoCurrency[];
  worstPerformers: CryptoCurrency[];
  topByVolume: CryptoCurrency[];
  marketMetrics?: Record<string, number>;
}

// News item related to cryptocurrencies
export interface CryptoNewsItem {
  id: string;
  title: string;
  summary?: string;
  description?: string;
  url: string;
  source: string;
  imageUrl?: string;
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

// AI-generated trading recommendation
export interface TradeRecommendation {
  id: string;
  coinId: string;
  coinName: string;
  coinSymbol: string;
  type: 'Buy' | 'Sell' | 'Hold';
  price: number;
  targetPrice?: number;
  stopLoss?: number;
  confidence: 'low' | 'medium' | 'high';
  reasoning?: string;
  timestamp: Date;
  expiryDate?: Date;
}