import {
  Component,
  OnInit,
  OnDestroy,
  AfterViewInit,
  ChangeDetectorRef,
  ViewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CryptoDataService } from '../../../services/crypto-data.service';
import { NewsService } from '../../../services/news.service';
import {
  ApexAxisChartSeries,
  NgApexchartsModule,
  ChartComponent,
  ApexOptions,
  ApexChart,
  ApexXAxis,
  ApexDataLabels,
  ApexStroke,
  ApexYAxis,
  ApexTitleSubtitle,
  ApexFill,
  ApexTooltip,
  ApexMarkers,
  ApexGrid,
  ApexLegend,
  ApexTheme,
  ApexAnnotations
} from 'ng-apexcharts';
import { NewsCardComponent } from '../../components/news-card/news-card.component';
import { FormatLargeNumberPipe } from '../../../shared/pipes/format-large-number.pipe';
import { Subscription } from 'rxjs';
import { formatLargeNumber } from '../../../domain/utils/crypto-utils';

// Define the chart options interface to ensure type safety
export interface ChartOptions {
  series: ApexAxisChartSeries;
  chart: ApexChart;
  xaxis: ApexXAxis;
  yaxis: ApexYAxis;
  title: ApexTitleSubtitle;
  stroke: ApexStroke;
  dataLabels: ApexDataLabels;
  fill: ApexFill;
  tooltip: ApexTooltip;
  markers: ApexMarkers;
  grid: ApexGrid;
  legend: ApexLegend;
  theme: ApexTheme;
  annotations: ApexAnnotations;
}

@Component({
  selector: 'app-coin-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, NgApexchartsModule, FormsModule, NewsCardComponent, FormatLargeNumberPipe],
  templateUrl: './coin-detail.component.html',
  styleUrls: ['./coin-detail.component.scss'],
})
export class CoinDetailComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('chartObj') chart!: ChartComponent;
  formatLargeNumber = formatLargeNumber;
  indicatorMeta: { [key: string]: { description: string; weight: number; meaning: string } } = {};
  isLoading = true;
  error: string | null = null;
  coinId: string = '';
  // Flag to track if technical analysis data has been loaded
  technicalAnalysisLoaded = false;
  coinData: any = {
    id: '',
    symbol: '',
    name: '',
    imageUrl: '',
    currentPrice: 0,
    marketCap: 0,
    priceChangePercentage24h: 0,
    marketCapRank: 0,
    volume24h: 0,
    circulatingSupply: 0,
    totalSupply: 0,
    maxSupply: 0,
    allTimeHigh: 0,
    allTimeHighDate: '',
    allTimeLow: 0,
    allTimeLowDate: '',
    high24h: 0,
    low24h: 0,
    homepage: '',
    whitepaper: '',
    blockchainSite: '',
    twitter: '',
    facebook: '',
    subreddit: '',
    categories: [],
    hashingAlgorithm: '',
    genesisDate: '',
    sentimentVotesUpPercentage: 0,
    sentimentVotesDownPercentage: 0,
    description: '',
  };
  technicalAnalysis: any = null;
  relatedNews: any[] = [];
  selectedDays = 30;

  // Technical indicators for chart
  indicatorData: { [key: string]: any[] } = {};
  enabledIndicators: { [key: string]: boolean } = {
    sma_14: false,
    ema_14: false,
    bollinger_upper: false,
    bollinger_middle: false,
    bollinger_lower: false,
    rsi: false,
    macd: false,
  };
  showIndicatorPanel = false;

  // Indicator series colors
  indicatorColors: { [key: string]: string } = {
    sma_14: '#2E93fA',
    ema_14: '#FF9800',
    bollinger_upper: '#4CAF50',
    bollinger_middle: '#9C27B0',
    bollinger_lower: '#4CAF50',
    rsi: '#F44336',
    macd: '#03A9F4',
  };

  // Properly typed chart options with non-nullable values
  chartOptions: ChartOptions = {
    series: [{
      name: 'Price',
      data: []
    }],
    chart: {
      type: 'line',
      height: 350,
      zoom: { enabled: true },
      toolbar: { show: true },
    },
    dataLabels: { enabled: false },
    stroke: { curve: 'smooth', width: 3 },
    title: { text: 'Price Chart', align: 'left' },
    xaxis: {
      type: 'datetime',
      title: { text: 'Date' },
      labels: { 
        datetimeUTC: true,
        formatter: function(val) {
          const date = new Date(val);
          return date.toLocaleDateString();
        }
      }
    },
    yaxis: { 
      title: { text: 'Price (USD)' },
      labels: {
        formatter: function(val) {
          return '$' + val.toFixed(2);
        }
      }
    },
    tooltip: {
      x: {
        format: 'dd MMM yyyy'
      },
      y: {
        formatter: function(val) {
          return '$' + val.toFixed(2);
        }
      }
    },
    fill: {
      type: 'solid'
    },
    markers: { 
      size: 0
    },
    grid: {
      show: true
    },
    legend: {
      show: true
    },
    theme: {
      mode: 'light'
    },
    annotations: {
      // Default empty annotations
      yaxis: [],
      xaxis: [],
      points: []
    }
  };

  chartData: any;

  private subscriptions: Subscription[] = [];

  constructor(
    private route: ActivatedRoute,
    private cryptoService: CryptoDataService,
    private newsService: NewsService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.subscriptions.push(
      this.route.params.subscribe((params) => {
        this.coinId = params['id'];
        this.loadCoinData();
      })
    );
  }

  ngAfterViewInit(): void {
    this.loadChartData();

    // Enable Bootstrap tooltips
    const tooltipTriggerList = Array.from(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach((tooltipTriggerEl) => {
      // @ts-ignore
      new window.bootstrap.Tooltip(tooltipTriggerEl);
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach((sub) => sub.unsubscribe());
  }

  loadCoinData(): void {
    this.isLoading = true;
    this.error = null;
    this.cryptoService.getCoinDetails(this.coinId).subscribe({
      next: (data) => {
        this.coinData = {
          id: data.id,
          symbol: data.symbol,
          name: data.name,
          imageUrl: data.imageUrl,
          currentPrice: data.currentPrice,
          marketCap: data.marketCap,
          priceChangePercentage24h: data.priceChangePercentage24h,
          marketCapRank: data.marketCapRank,
          volume24h: data.volume24h,
          circulatingSupply: data.circulatingSupply,
          totalSupply: data.totalSupply,
          maxSupply: data.maxSupply,
          allTimeHigh: data.allTimeHigh,
          allTimeHighDate: data.allTimeHighDate,
          allTimeLow: data.allTimeLow,
          allTimeLowDate: data.allTimeLowDate,
          high24h: data.high24h,
          low24h: data.low24h,
          homepage: data.homepage,
          whitepaper: data.whitepaper,
          blockchainSite: data.blockchainSite,
          twitter: data.twitter,
          facebook: data.facebook,
          subreddit: data.subreddit,
          categories: data.categories,
          hashingAlgorithm: data.hashingAlgorithm,
          genesisDate: data.genesisDate,
          sentimentVotesUpPercentage: data.sentimentVotesUpPercentage,
          sentimentVotesDownPercentage: data.sentimentVotesDownPercentage,
          description: data.description,
        };
        this.loadTechnicalAnalysis();
        this.loadRelatedNews();
      },
      error: (error) => {
        console.error('Error fetching coin details:', error);
        this.error = 'Failed to load cryptocurrency details.';
        this.isLoading = false;
      },
    });
  }

  loadChartData(): void {
    // Determine which indicators to request based on enabled indicators
    const enabledIndicatorStrings: string[] = [];
    
    Object.entries(this.enabledIndicators).forEach(([key, enabled]) => {
      if (enabled) {
        const [type, period] = key.split('_');
        if (type === 'sma' || type === 'ema') {
          enabledIndicatorStrings.push(`${type}:${period || '20'}`);
        } else if (type === 'rsi') {
          enabledIndicatorStrings.push(`${type}:${period || '14'}`);
        } else if (key === 'macd') {
          enabledIndicatorStrings.push('macd');
        } else if (key.startsWith('bollinger')) {
          // Only add bollinger once regardless of which band is enabled
          if (!enabledIndicatorStrings.some(i => i.startsWith('bollinger'))) {
            enabledIndicatorStrings.push('bollinger:20:2');
          }
        }
      }
    });
    
    // Use extended chart data with indicators
    this.cryptoService.getEnhancedChart(
      this.coinId, 
      this.selectedDays, 
      enabledIndicatorStrings.length > 0 ? enabledIndicatorStrings : undefined
    ).subscribe({
      next: (data) => {
        this.chartData = data;
        this.updateChart(data);
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error fetching enhanced chart data:', error);
        
        // If the enhanced chart fails (likely due to insufficient data), fall back to regular chart
        if (error.status === 422) {
          this.error = 'Insufficient historical data for the requested indicators. Using basic chart instead.';
          
          // Fallback to regular chart
          this.cryptoService.getMarketChart(this.coinId, this.selectedDays).subscribe({
            next: (data) => {
              this.updateChart(data);
              this.isLoading = false;
            },
            error: (fallbackError) => {
              console.error('Error fetching market chart:', fallbackError);
              this.error = 'Failed to load price chart data.';
              this.isLoading = false;
            },
          });
        } else {
          this.error = 'Failed to load enhanced chart data.';
          this.isLoading = false;
        }
      },
    });
  }

  loadTechnicalAnalysis(): void {
    if (!this.coinId || this.technicalAnalysisLoaded) return;
    console.log('Loading technical analysis for:', this.coinId);
    this.cryptoService.getTechnicalAnalysis(this.coinId).subscribe({
      next: (data) => {
        this.technicalAnalysis = data;
        this.technicalAnalysisLoaded = true;

        // Process indicator data for chart
        this.processIndicatorData(data);

        // Extract indicator meta info if present
        if (data && data.indicatorMeta) {
          this.indicatorMeta = data.indicatorMeta;
        }
      },
      error: (error) => {
        console.error('Error fetching technical analysis:', error);
      },
    });
  }

  loadRelatedNews(): void {
    if (!this.coinData?.symbol) return;

    this.newsService.getCoinNews(this.coinId, this.coinData.symbol, 6).subscribe({
      next: (data) => {
        this.relatedNews = data;
      },
      error: (error) => {
        console.error('Error fetching related news:', error);
      },
    });
  }

  updateChartPeriod(days: number): void {
    this.selectedDays = days;
    this.loadChartData();
  }

  private updateChart(data: any): void {
    // Transform data for ApexCharts
    const prices = data.prices.map((point: any) => ({
      x: new Date(point.timestamp),
      y: point.price,
    }));

    // Create the new options object
    const newOptions: Partial<ApexOptions> = {
      series: [
        {
          name: `${this.coinData.name || 'Price'} Price`,
          data: prices,
          color: '#1A73E8',
          type: 'line',
        },
      ],
      title: { text: `${this.coinData.name || 'Cryptocurrency'} Price Chart`, align: 'left' },
      // ...existing code...
    };

    // Process indicator data if available directly from the enhanced chart endpoint
    if (data.indicatorSeries && Object.keys(data.indicatorSeries).length > 0) {
      this.indicatorData = {};
      
      // Process each indicator from the server
      Object.keys(data.indicatorSeries).forEach(key => {
        const series = data.indicatorSeries[key];
        if (series && series.length > 0) {
          // Convert to chart format and map to our indicator names
          const mappedKey = this.mapIndicatorKey(key);
          if (mappedKey) {
            this.indicatorData[mappedKey] = series.map((point: any) => ({
              x: new Date(point.timestamp),
              y: point.value
            }));
          }
        }
      });
      
      console.log('Processed indicator data:', Object.keys(this.indicatorData));
    }

    // Assign to trigger change detection
    this.chartOptions = { ...this.chartOptions, ...newOptions } as ChartOptions;

    // Update indicators on the chart if available
    if (Object.keys(this.indicatorData).length > 0) {
      this.updateChartWithIndicators();
    }

    this.cdr.detectChanges(); // Trigger change detection manually just in case
  }

  // Process technical indicators for displaying on chart
  processIndicatorData(data: any): void {
    if (!data) return;

    // Process indicator series
    if (data.indicatorSeries && Object.keys(data.indicatorSeries).length > 0) {
      Object.keys(data.indicatorSeries).forEach((key) => {
        const series = data.indicatorSeries[key];
        if (series && series.length > 0) {
          const mappedKey = this.mapIndicatorKey(key);
          if (mappedKey) {
            this.indicatorData[mappedKey] = series.map((point: any) => ({
              x: new Date(point.timestamp),
              y: point.value,
            }));
          }
        }
      });
    }

    // Process support and resistance levels
    if (data.supportLevels) {
      this.technicalAnalysis.supportLevels = data.supportLevels;
    }
    if (data.resistanceLevels) {
      this.technicalAnalysis.resistanceLevels = data.resistanceLevels;
    }

    // Update chart with indicators if any are enabled
    if (Object.keys(this.indicatorData).length > 0) {
      this.updateChartWithIndicators();
    }
  }

  toggleIndicator(indicator: string): void {
    this.enabledIndicators[indicator] = !this.enabledIndicators[indicator];
    this.updateChartWithIndicators();
  }

  toggleIndicatorPanel(): void {
    this.showIndicatorPanel = !this.showIndicatorPanel;
  }

  updateChartWithIndicators(): void {
    if (!this.chartOptions.series || !Array.isArray(this.chartOptions.series) || this.chartOptions.series.length === 0) return;

    const mainSeries = this.chartOptions.series[0];
    const newSeries = [mainSeries];

    Object.keys(this.enabledIndicators).forEach((indicator) => {
      if (this.enabledIndicators[indicator] && this.indicatorData[indicator]) {
        // handle bollinger bands separately
        if(!indicator.includes('bollinger')) {
          const seriesConfig: any = {
            name: this.formatIndicatorName(indicator),
            data: this.indicatorData[indicator],
            color: this.indicatorColors[indicator],
            type: 'line',
          };
  
          if (indicator === 'rsi') {
            seriesConfig.yAxisIndex = 1;
          }
  
          if (indicator.includes('bollinger')) {
            seriesConfig.stroke = { width: 2, dashArray: 5 };
          }
  
          newSeries.push(seriesConfig);
        }else{
  
          newSeries.push({
            name: 'Bollinger Upper Band',
            data: this.indicatorData['bollinger_upper'],
            color: this.indicatorColors['bollinger_upper'],
            type: 'line',
          });
  
          newSeries.push({
            name: 'Bollinger Middle Band',
            data: this.indicatorData['bollinger_middle'],
            color: this.indicatorColors['bollinger_middle'],
            type: 'line',
          });
  
          newSeries.push({
            name: 'Bollinger Lower Band',
            data: this.indicatorData['bollinger_lower'],
            color: this.indicatorColors['bollinger_lower'],
            type: 'line',
          });
        }

        
      }
    });

    this.chartOptions.series = newSeries as ApexAxisChartSeries;

    if (this.enabledIndicators['rsi']) {
      this.chartOptions.annotations = {
        yaxis: [
          { y: 70, borderColor: '#FF4560', label: { text: 'Overbought', style: { color: '#fff', background: '#FF4560' } } },
          { y: 30, borderColor: '#00E396', label: { text: 'Oversold', style: { color: '#fff', background: '#00E396' } } },
        ],
      };
    } else {
      this.chartOptions.annotations = {
        yaxis: [],
        xaxis: [],
        points: []
      };
    }

    this.cdr.detectChanges();
  }

  formatIndicatorName(indicator: string): string {
    return indicator
      .replace('_', ' ')
      .replace('bollinger', 'Bollinger Band')
      .replace('sma', 'SMA')
      .replace('ema', 'EMA')
      .replace('rsi', 'RSI')
      .replace('macd', 'MACD')
      .toUpperCase();
  }

  getIndicators(): any[] {
    return this.technicalAnalysis?.indicators || [];
  }

  getIndicatorGroups(): any[] {
    return this.technicalAnalysis?.indicatorGroups || [];
  }

  getSignalClass(signal: any): string {
    if (!signal || typeof signal !== 'string') {
      return 'text-secondary';
    }
    const normalized = signal.toLowerCase();
    if (normalized === 'buy' || normalized === 'bullish') {
      return 'text-success';
    }
    if (normalized === 'sell' || normalized === 'bearish') {
      return 'text-danger';
    }
    if (normalized === 'neutral' || normalized === 'hold') {
      return 'text-warning';
    }
    return 'text-secondary';
  }

  getSignalIcon(signal: string): string {
    if (!signal) return 'bi bi-dash-circle';
    
    signal = signal.toLowerCase();
    if (signal === 'buy' || signal === 'bullish') {
      return 'bi bi-arrow-up-circle-fill';
    } else if (signal === 'sell' || signal === 'bearish') {
      return 'bi bi-arrow-down-circle-fill';
    } else {
      return 'bi bi-dash-circle';
    }
  }
  
  getTrendIcon(trend: string): string {
    if (!trend) return 'bi bi-dash';
    
    trend = trend.toLowerCase();
    if (trend === 'bullish') {
      return 'bi bi-graph-up-arrow';
    } else if (trend === 'bearish') {
      return 'bi bi-graph-down-arrow';
    } else {
      return 'bi bi-arrows-collapse';
    }
  }
  
  hasMovingAverages(): boolean {
    return this.technicalAnalysis?.movingAverages 
      && Object.keys(this.technicalAnalysis.movingAverages).length > 0;
  }
  
  hasOscillators(): boolean {
    return this.technicalAnalysis?.oscillators 
      && Object.keys(this.technicalAnalysis.oscillators).length > 0;
  }
  
  hasVolatilityIndicators(): boolean {
    return this.technicalAnalysis?.volatilityIndicators 
      && Object.keys(this.technicalAnalysis.volatilityIndicators).length > 0;
  }
  
  hasVolumeIndicators(): boolean {
    return this.technicalAnalysis?.volumeIndicators 
      && Object.keys(this.technicalAnalysis.volumeIndicators).length > 0;
  }
  
  getMovingAveragesDescription(): string {
    return this.technicalAnalysis?.movingAverages?.['description'] as string;
  }
  
  getMovingAveragesMeaning(): string {
    return this.technicalAnalysis?.movingAverages?.['meaning'] as string;
  }
  
  getMovingAveragesIndicators(): any[] {
    return this.technicalAnalysis?.movingAverages?.['indicators'] as any[] || [];
  }
  
  getOscillatorsDescription(): string {
    return this.technicalAnalysis?.oscillators?.['description'] as string;
  }
  
  getOscillatorsMeaning(): string {
    return this.technicalAnalysis?.oscillators?.['meaning'] as string;
  }
  
  getOscillatorsIndicators(): any[] {
    return this.technicalAnalysis?.oscillators?.['indicators'] as any[] || [];
  }
  
  getVolatilityDescription(): string {
    return this.technicalAnalysis?.volatilityIndicators?.['description'] as string;
  }
  
  getVolatilityMeaning(): string {
    return this.technicalAnalysis?.volatilityIndicators?.['meaning'] as string;
  }
  
  getVolatilityIndicators(): any[] {
    return this.technicalAnalysis?.volatilityIndicators?.['indicators'] as any[] || [];
  }
  
  getVolumeDescription(): string {
    return this.technicalAnalysis?.volumeIndicators?.['description'] as string;
  }
  
  getVolumeMeaning(): string {
    return this.technicalAnalysis?.volumeIndicators?.['meaning'] as string;
  }
  
  getVolumeIndicators(): any[] {
    return this.technicalAnalysis?.volumeIndicators?.['indicators'] as any[] || [];
  }
  
  getSupportLevels(): number[] {
    return this.technicalAnalysis?.metaData?.['SupportLevels'] as number[] || [];
  }
  
  getResistanceLevels(): number[] {
    return this.technicalAnalysis?.metaData?.['ResistanceLevels'] as number[] || [];
  }
  
  getSignalsList(): any[] {
    return this.technicalAnalysis?.metaData?.['SignalsList'] as any[] || [];
  }
  
  getIndicatorTooltip(name: string): string {
    const tooltips: {[key: string]: string} = {
      'RSI14': 'Relative Strength Index measures overbought/oversold conditions. Above 70 is overbought, below 30 is oversold.',
      'MACD': 'Moving Average Convergence Divergence shows the relationship between two moving averages of a price.',
      'Bollinger Bands Width': 'Measures volatility. Wider bands indicate higher volatility, narrower bands indicate lower volatility.',
      'SMA20': '20-period Simple Moving Average',
      'SMA50': '50-period Simple Moving Average',
      'SMA200': '200-period Simple Moving Average',
      'EMA12': '12-period Exponential Moving Average',
      'EMA26': '26-period Exponential Moving Average'
    };
    
    return tooltips[name] || `Technical indicator: ${name}`;
  }
  
  hasRsiData(): boolean {
    const latestValues = this.technicalAnalysis?.metaData?.['LatestValues'] as any;
    return latestValues && latestValues['RSI14'] !== undefined;
  }
  
  getRsiPercentage(): number {
    const latestValues = this.technicalAnalysis?.metaData?.['LatestValues'] as any;
    if (!latestValues || latestValues['RSI14'] === undefined) {
      return 50;
    }
    
    return latestValues['RSI14'];
  }

  /**
   * Maps indicator keys from backend format to frontend format
   */
  private mapIndicatorKey(key: string): string | null {
    // Direct mappings
    const directMappings: {[key: string]: string} = {
      'SMA14': 'sma_14',
      'SMA_50': 'sma_50',
      'EMA14': 'ema_14',
      'RSI14': 'rsi',
      'MACD': 'macd',
      'BBANDS_UPPER': 'bollinger_upper',
      'BBANDS_MIDDLE': 'bollinger_middle',
      'BBANDS_LOWER': 'bollinger_lower'
    };
    
    // Check direct mappings first
    if (directMappings[key]) {
      return directMappings[key];
    }
    
    // Handle variations in bollinger bands naming
    if (key.includes('BBANDS')) {
      if (key.includes('upper')) return 'bollinger_upper';
      if (key.includes('middle')) return 'bollinger_middle';
      if (key.includes('lower')) return 'bollinger_lower';
    }
    
    // Handle variations in MACD naming
    if (key.includes('MACD') && key.includes('line')) {
      return 'macd';
    }
    
    // Handle RSI with different periods
    if (key.startsWith('RSI')) {
      return 'rsi';
    }
    
    // Fall back to original key if no mapping found
    return key;
  }

  /**
   * Creates a flat line indicator series based on price timestamps
   */
  private createFlatLineIndicator(prices: any[], value: number): any[] {
    if (!prices || prices.length === 0 || value === undefined) {
      return [];
    }
    
    // Create a series with the same value across all price timestamps
    return prices.map((point: any) => ({
      x: new Date(point.timestamp),
      y: value
    }));
  }
}
