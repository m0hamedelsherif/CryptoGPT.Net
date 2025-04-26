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
import { CryptoDataService } from '../../services/crypto-data.service';
import { NewsService } from '../../services/news.service';
import {
  ApexAxisChartSeries,
  NgApexchartsModule,
  ChartComponent,
  ApexOptions
} from 'ng-apexcharts';
import { TooltipModule } from 'ngx-bootstrap/tooltip';

@Component({
  selector: 'app-coin-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, NgApexchartsModule, TooltipModule, FormsModule],
  templateUrl: './coin-detail.component.html',
  styleUrls: ['./coin-detail.component.css'],
})
export class CoinDetailComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('chartObj') chart!: ChartComponent;
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

  chartOptions: ApexOptions = {
    series: [],
    chart: {
      type: 'line',
      height: 350,
      zoom: { enabled: true },
      toolbar: { show: true },
    },
    dataLabels: { enabled: false },
    stroke: { curve: 'smooth', width: 3 },
    title: { text: '', align: 'left' },
    xaxis: {
      type: 'datetime',
      title: { text: 'Date' },
      labels: {
        datetimeUTC: true,
        format: 'dd MMM yyyy',
        formatter: (value: string, timestamp?: number, opts?: any) => {
          const ts = timestamp ?? Number(value);
          if (!ts) return '';
          const date = new Date(ts);
          if (this.selectedDays <= 7) {
            const day = date.getDate().toString().padStart(2, '0');
            const month = date.toLocaleString('en-US', { month: 'short' });
            const hour = date.getHours().toString().padStart(2, '0');
            const minute = date.getMinutes().toString().padStart(2, '0');
            return `${day} ${month} ${hour}:${minute}`;
          } else {
            const day = date.getDate().toString().padStart(2, '0');
            const month = date.toLocaleString('en-US', { month: 'short' });
            const year = date.getFullYear();
            return `${day} ${month} ${year}`;
          }
        },
      },
    },
    yaxis: {
      title: { text: 'Price (USD)' },
      labels: {
        formatter: (val: number) => {
          if (val >= 1e12) return (val / 1e12).toFixed(2) + 'T';
          if (val >= 1e9) return (val / 1e9).toFixed(2) + 'B';
          if (val >= 1e6) return (val / 1e6).toFixed(2) + 'M';
          if (val >= 1e3) return (val / 1e3).toFixed(2) + 'K';
          return val.toFixed(2);
        },
      },
    },
    tooltip: {
      x: {
        format: 'dd MMM yyyy',
        formatter: (value: number, opts?: any) => {
          const ts = Number(value);
          if (!ts) return '';
          const date = new Date(ts);
          if (this.selectedDays <= 7) {
            const day = date.getDate().toString().padStart(2, '0');
            const month = date.toLocaleString('en-US', { month: 'short' });
            const hour = date.getHours().toString().padStart(2, '0');
            const minute = date.getMinutes().toString().padStart(2, '0');
            return `${day} ${month} ${hour}:${minute}`;
          } else {
            const day = date.getDate().toString().padStart(2, '0');
            const month = date.toLocaleString('en-US', { month: 'short' });
            const year = date.getFullYear();
            return `${day} ${month} ${year}`;
          }
        },
      },
      y: {
        formatter: (val: number) => {
          if (val >= 1e12) return '$' + (val / 1e12).toFixed(2) + 'T';
          if (val >= 1e9) return '$' + (val / 1e9).toFixed(2) + 'B';
          if (val >= 1e6) return '$' + (val / 1e6).toFixed(2) + 'M';
          if (val >= 1e3) return '$' + (val / 1e3).toFixed(2) + 'K';
          return '$' + val.toFixed(2);
        },
      },
    },
    fill: {
      type: 'solid',
      gradient: {
        shade: 'dark',
        type: 'vertical',
        opacityFrom: 0.8,
        opacityTo: 0.2,
        colorStops: [
          {
            offset: 0,
            color: "#1A73E8",
            opacity: 0.6
          },
          {
            offset: 100,
            color: "#1A73E8",
            opacity: 0.1
          }
        ]
      },
    },
    markers: { 
      size: 0,
      hover: {
        size: 5,
        sizeOffset: 3
      }
    },
    theme: { mode: 'light' },
    legend: { show: true },
    grid: {
      borderColor: '#e7e7e7',
      row: { colors: ['#f3f3f3', 'transparent'], opacity: 0.5 },
    },
    responsive: [
      {
        breakpoint: 600,
        options: { chart: { height: 250 } },
      },
    ],
    annotations: {},
  };
  chartData: any;

  constructor(
    private route: ActivatedRoute,
    private cryptoService: CryptoDataService,
    private newsService: NewsService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe((params) => {
      this.coinId = params['id'];
      this.loadCoinData();
    });
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
    // No chart.js cleanup needed
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
    if (!this.coinData?.symbol || this.technicalAnalysisLoaded) return;

    this.cryptoService.getTechnicalAnalysis(this.coinData.symbol).subscribe({
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
      
      // Map the server indicator names to our frontend indicator names
      const indicatorMapping: { [key: string]: string } = {
        'sma_20': 'sma_14',
        'ema_20': 'ema_14',
        'bollinger_upper': 'bollinger_upper',
        'bollinger_middle': 'bollinger_middle', 
        'bollinger_lower': 'bollinger_lower',
        'rsi_14': 'rsi',
        'macd_line': 'macd'
      };
      
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
    this.chartOptions = { ...this.chartOptions, ...newOptions };

    // Update indicators on the chart if available
    if (Object.keys(this.indicatorData).length > 0) {
      this.updateChartWithIndicators();
    }

    this.cdr.detectChanges(); // Trigger change detection manually just in case
  }

  // Process technical indicators for displaying on chart
  processIndicatorData(data: any): void {
    if (!data) return;
    
    // If the backend provides time-series data for each indicator, use it
    if (data.indicatorSeries && Object.keys(data.indicatorSeries).length > 0) {
      // Process each time series
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
      
      // Update the chart with the indicator data
      this.updateChartWithIndicators();
      return;
    }
    
    // If we have indicatorGroups, process them
    if (data.indicatorGroups && data.indicatorGroups.length > 0) {
      data.indicatorGroups.forEach((group: any) => {
        if (!group.indicators) return;
        
        // Process each indicator type
        switch (group.type.toLowerCase()) {
          case 'sma':
            const sma14 = group.indicators.find((i: any) => i.name === 'sma_14');
            if (sma14 && sma14.value) {
              // Use timestamps from price chart for proper display
              if (this.chartData && this.chartData.prices && this.chartData.prices.length > 0) {
                this.indicatorData['sma_14'] = this.createFlatLineIndicator(
                  this.chartData.prices, 
                  sma14.value
                );
              }
            }
            break;
            
          case 'ema':
            const ema14 = group.indicators.find((i: any) => i.name === 'ema_14');
            if (ema14 && ema14.value) {
              if (this.chartData && this.chartData.prices && this.chartData.prices.length > 0) {
                this.indicatorData['ema_14'] = this.createFlatLineIndicator(
                  this.chartData.prices, 
                  ema14.value
                );
              }
            }
            break;
            
          case 'bollinger':
            const upper = group.indicators.find((i: any) => i.name === 'upper_band');
            const middle = group.indicators.find((i: any) => i.name === 'middle_band');
            const lower = group.indicators.find((i: any) => i.name === 'lower_band');
            
            if (this.chartData && this.chartData.prices && this.chartData.prices.length > 0) {
              if (upper && upper.value) {
                this.indicatorData['bollinger_upper'] = this.createFlatLineIndicator(
                  this.chartData.prices,
                  upper.value
                );
              }
              
              if (middle && middle.value) {
                this.indicatorData['bollinger_middle'] = this.createFlatLineIndicator(
                  this.chartData.prices,
                  middle.value
                );
              }
              
              if (lower && lower.value) {
                this.indicatorData['bollinger_lower'] = this.createFlatLineIndicator(
                  this.chartData.prices,
                  lower.value
                );
              }
            }
            break;
            
          case 'rsi':
            const rsi = group.indicators.find((i: any) => i.name === 'rsi');
            if (rsi && rsi.value) {
              if (this.chartData && this.chartData.prices && this.chartData.prices.length > 0) {
                this.indicatorData['rsi'] = this.createFlatLineIndicator(
                  this.chartData.prices,
                  rsi.value
                );
              }
            }
            break;
            
          case 'macd':
            const macd = group.indicators.find((i: any) => i.name === 'macd_line');
            if (macd && macd.value) {
              if (this.chartData && this.chartData.prices && this.chartData.prices.length > 0) {
                this.indicatorData['macd'] = this.createFlatLineIndicator(
                  this.chartData.prices,
                  macd.value
                );
              }
            }
            break;
        }
      });
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

    // Start with the price series
    const mainSeries = this.chartOptions.series[0];
    const newSeries = [mainSeries];
    
    // Check if RSI is enabled to determine if we need a secondary y-axis
    const rsiEnabled = this.enabledIndicators['rsi'] && this.indicatorData['rsi'];
    
    // Configure chart options for RSI if enabled
    let updatedOptions: Partial<ApexOptions> = {};
    if (rsiEnabled) {
      updatedOptions = {
        chart: {
          type: 'line',
          height: 500, // Increase height to accommodate the secondary chart
          stacked: false
        },
        yaxis: [
          {
            // Primary y-axis for price
            title: {
              text: 'Price (USD)'
            },
            labels: {
              formatter: (val: number) => {
                if (val >= 1e12) return (val / 1e12).toFixed(2) + 'T';
                if (val >= 1e9) return (val / 1e9).toFixed(2) + 'B';
                if (val >= 1e6) return (val / 1e6).toFixed(2) + 'M';
                if (val >= 1e3) return (val / 1e3).toFixed(2) + 'K';
                return val.toFixed(2);
              }
            }
          },
          {
            // Secondary y-axis for RSI
            opposite: true,
            title: {
              text: 'RSI (0-100)'
            },
            min: 0,
            max: 100,
            tickAmount: 5,
            labels: {
              formatter: (val: number) => val.toFixed(0)
            },
            forceNiceScale: true
          }
        ]
      };
    } else {
      // Reset to default configuration if RSI is disabled
      updatedOptions = {
        chart: {
          type: 'line',
          height: 350,
          stacked: false
        },
        yaxis: {
          title: {
            text: 'Price (USD)'
          },
          labels: {
            formatter: (val: number) => {
              if (val >= 1e12) return (val / 1e12).toFixed(2) + 'T';
              if (val >= 1e9) return (val / 1e9).toFixed(2) + 'B';
              if (val >= 1e6) return (val / 1e6).toFixed(2) + 'M';
              if (val >= 1e3) return (val / 1e3).toFixed(2) + 'K';
              return val.toFixed(2);
            }
          }
        }
      };
    }

    // Add enabled indicator series
    Object.keys(this.enabledIndicators).forEach((indicator) => {
      if (this.enabledIndicators[indicator] && this.indicatorData[indicator]) {
        const seriesConfig: any = {
          name: this.formatIndicatorName(indicator),
          data: this.indicatorData[indicator],
          color: this.indicatorColors[indicator],
          type: 'line'
        };
        
        // Special treatment for RSI - assign to secondary y-axis
        if (indicator === 'rsi') {
          seriesConfig.yAxisIndex = 1; // Use secondary y-axis for RSI
        }
        
        // Only add dashed style for Bollinger bands
        if (indicator.includes('bollinger')) {
          seriesConfig.stroke = { 
            width: 2,
            dashArray: 5
          };
        }
        
        newSeries.push(seriesConfig);
      }
    });

    // Update chart series and options with proper type casting
    this.chartOptions.series = newSeries as ApexAxisChartSeries;
    this.chartOptions = { ...this.chartOptions, ...updatedOptions };
    
    // Add RSI threshold lines if RSI is enabled
    if (rsiEnabled) {
      this.chartOptions.annotations = {
        yaxis: [
          {
            y: 70,
            borderColor: '#FF4560',
            label: {
              borderColor: '#FF4560',
              style: {
                color: '#fff',
                background: '#FF4560'
              },
              text: 'Overbought'
            }
          },
          {
            y: 30,
            borderColor: '#00E396',
            label: {
              borderColor: '#00E396',
              style: {
                color: '#fff',
                background: '#00E396'
              },
              text: 'Oversold'
            }
          }
        ]
      };
    } else {
      // Reset annotations if RSI is disabled
      this.chartOptions.annotations = {};
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

  formatLargeNumber(value: number): string {
    if (!value) return '0';

    if (value >= 1e12) {
      return (value / 1e12).toFixed(2) + ' T';
    } else if (value >= 1e9) {
      return (value / 1e9).toFixed(2) + ' B';
    } else if (value >= 1e6) {
      return (value / 1e6).toFixed(2) + ' M';
    } else if (value >= 1e3) {
      return (value / 1e3).toFixed(2) + ' K';
    } else {
      return value.toFixed(2);
    }
  }

  getIndicatorDescription(name: string, meta?: any): string {
    if (meta && meta.description) return meta.description;
    if (!name) return 'No description available for this indicator.';
    return 'No description available for this indicator.';
  }

  getIndicatorWeight(meta?: any): string {
    return meta && meta.weight ? `Weight: ${meta.weight}` : '';
  }

  getIndicatorMeaning(meta?: any): string {
    return meta && meta.meaning ? meta.meaning : '';
  }

  /**
   * Maps indicator keys from backend format to frontend format
   */
  private mapIndicatorKey(key: string): string | null {
    // Direct mappings
    const directMappings: {[key: string]: string} = {
      'sma_20': 'sma_14',
      'sma_50': 'sma_50',
      'ema_20': 'ema_14',
      'rsi_14': 'rsi',
      'macd_line': 'macd',
      'bollinger_upper': 'bollinger_upper',
      'bollinger_middle': 'bollinger_middle',
      'bollinger_lower': 'bollinger_lower'
    };
    
    // Check direct mappings first
    if (directMappings[key]) {
      return directMappings[key];
    }
    
    // Handle variations in bollinger bands naming
    if (key.includes('bollinger')) {
      if (key.includes('upper')) return 'bollinger_upper';
      if (key.includes('middle')) return 'bollinger_middle';
      if (key.includes('lower')) return 'bollinger_lower';
    }
    
    // Handle variations in MACD naming
    if (key.includes('macd') && key.includes('line')) {
      return 'macd';
    }
    
    // Handle RSI with different periods
    if (key.startsWith('rsi_')) {
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
