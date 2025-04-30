import { Injectable } from '@angular/core';
import { CryptoCurrency, PriceHistoryPoint } from '../models/crypto-currency.model';

/**
 * Domain service for cryptocurrency analytics
 * This service contains pure business logic for analyzing crypto data
 * It does not depend on any infrastructure or application services
 */
@Injectable({
  providedIn: 'root'
})
export class CryptoAnalyticsService {
  
  /**
   * Calculate volatility for a cryptocurrency based on price history
   * @param priceHistory Array of price history points
   * @returns Volatility as a decimal (e.g., 0.05 = 5% volatility)
   */
  calculateVolatility(priceHistory: PriceHistoryPoint[]): number {
    if (priceHistory.length < 2) {
      return 0;
    }
    
    // Calculate daily returns
    const returns: number[] = [];
    for (let i = 1; i < priceHistory.length; i++) {
      const previousPrice = priceHistory[i - 1].price;
      const currentPrice = priceHistory[i].price;
      const dailyReturn = (currentPrice - previousPrice) / previousPrice;
      returns.push(dailyReturn);
    }
    
    // Calculate standard deviation of returns (volatility)
    const mean = returns.reduce((sum, value) => sum + value, 0) / returns.length;
    const variance = returns.reduce((sum, value) => sum + Math.pow(value - mean, 2), 0) / returns.length;
    
    return Math.sqrt(variance);
  }
  
  /**
   * Calculate moving average for a cryptocurrency
   * @param priceHistory Array of price history points
   * @param period Number of days to include in the moving average
   * @returns Array of moving average values
   */
  calculateMovingAverage(priceHistory: PriceHistoryPoint[], period: number): number[] {
    if (priceHistory.length < period) {
      return [];
    }
    
    const movingAverages: number[] = [];
    
    for (let i = period - 1; i < priceHistory.length; i++) {
      let sum = 0;
      for (let j = 0; j < period; j++) {
        sum += priceHistory[i - j].price;
      }
      movingAverages.push(sum / period);
    }
    
    return movingAverages;
  }
  
  /**
   * Calculate Relative Strength Index (RSI)
   * @param priceHistory Array of price history points
   * @param period Period for RSI calculation (typically 14 days)
   * @returns RSI value between 0-100
   */
  calculateRSI(priceHistory: PriceHistoryPoint[], period: number = 14): number {
    if (priceHistory.length <= period) {
      return 50; // Default neutral value if not enough data
    }
    
    let gains = 0;
    let losses = 0;
    
    // Calculate initial average gains and losses
    for (let i = 1; i <= period; i++) {
      const change = priceHistory[i].price - priceHistory[i - 1].price;
      if (change >= 0) {
        gains += change;
      } else {
        losses += Math.abs(change);
      }
    }
    
    let avgGain = gains / period;
    let avgLoss = losses / period;
    
    // Calculate RSI using Wilder's smoothing method
    for (let i = period + 1; i < priceHistory.length; i++) {
      const change = priceHistory[i].price - priceHistory[i - 1].price;
      
      if (change >= 0) {
        avgGain = (avgGain * (period - 1) + change) / period;
        avgLoss = (avgLoss * (period - 1)) / period;
      } else {
        avgGain = (avgGain * (period - 1)) / period;
        avgLoss = (avgLoss * (period - 1) + Math.abs(change)) / period;
      }
    }
    
    if (avgLoss === 0) {
      return 100; // No losses, RSI is 100
    }
    
    const rs = avgGain / avgLoss;
    return 100 - (100 / (1 + rs));
  }
  
  /**
   * Determine investment risk level for a cryptocurrency
   * @param crypto Cryptocurrency data
   * @param priceHistory Price history for volatility calculation
   * @returns Risk level: 'low', 'medium', or 'high'
   */
  determineRiskLevel(crypto: CryptoCurrency, priceHistory: PriceHistoryPoint[]): 'low' | 'medium' | 'high' {
    // Calculate volatility
    const volatility = this.calculateVolatility(priceHistory);
    
    // Consider market cap (higher market cap generally means lower risk)
    const marketCapFactor = crypto.marketCap > 10000000000 ? -0.1 : // > $10B
                           crypto.marketCap > 1000000000 ? 0 :     // > $1B
                           crypto.marketCap > 100000000 ? 0.1 : 0.2; // < $100M = highest risk
    
    // Combine factors to determine risk
    const riskScore = volatility + marketCapFactor;
    
    if (riskScore < 0.05) return 'low';
    if (riskScore < 0.15) return 'medium';
    return 'high';
  }
  
  /**
   * Predict short-term trend based on technical indicators
   * @param priceHistory Price history data
   * @returns Predicted trend: 'bullish', 'bearish', or 'neutral'
   */
  predictShortTermTrend(priceHistory: PriceHistoryPoint[]): 'bullish' | 'bearish' | 'neutral' {
    if (priceHistory.length < 50) {
      return 'neutral'; // Not enough data
    }
    
    // Calculate moving averages
    const sma20 = this.calculateMovingAverage(priceHistory, 20).pop() || 0;
    const sma50 = this.calculateMovingAverage(priceHistory, 50).pop() || 0;
    
    // Calculate RSI
    const rsi = this.calculateRSI(priceHistory);
    
    // Current price
    const currentPrice = priceHistory[priceHistory.length - 1].price;
    
    // Determine trend based on multiple signals
    let signalCount = 0;
    
    // Price above SMA20 is bullish
    if (currentPrice > sma20) signalCount++;
    else signalCount--;
    
    // Price above SMA50 is bullish
    if (currentPrice > sma50) signalCount++;
    else signalCount--;
    
    // SMA20 above SMA50 is bullish (golden cross)
    if (sma20 > sma50) signalCount++;
    else signalCount--;
    
    // RSI signals
    if (rsi > 70) signalCount--; // Overbought
    else if (rsi < 30) signalCount++; // Oversold
    
    // Determine final trend
    if (signalCount > 1) return 'bullish';
    if (signalCount < -1) return 'bearish';
    return 'neutral';
  }
}