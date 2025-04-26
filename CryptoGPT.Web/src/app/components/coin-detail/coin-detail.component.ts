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
  ApexChart,
  ApexXAxis,
  ApexDataLabels,
  ApexStroke,
  ApexTitleSubtitle,
  ApexTooltip,
  ApexYAxis,
  ApexFill,
  ApexMarkers,
  ApexTheme,
  ApexLegend,
  ApexGrid,
  ApexResponsive,
  NgApexchartsModule,
  ChartComponent,
  ApexAnnotations,
} from 'ng-apexcharts';
import { TooltipModule } from 'ngx-bootstrap/tooltip';

export type ChartOptions = {
  series: ApexAxisChartSeries;
  chart: ApexChart;
  xaxis: ApexXAxis;
  yaxis: ApexYAxis;
  dataLabels: ApexDataLabels;
  stroke: ApexStroke;
  title: ApexTitleSubtitle;
  tooltip: ApexTooltip;
  fill: ApexFill;
  markers: ApexMarkers;
  theme: ApexTheme;
  legend: ApexLegend;
  grid: ApexGrid;
  responsive: ApexResponsive[];
  annotations: ApexAnnotations;
};

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

  chartOptions: ChartOptions = {
    series: [],
    chart: {
      type: 'line',
      height: 350,
      zoom: { enabled: true },
      toolbar: { show: true },
    },
    dataLabels: { enabled: false },
    stroke: { curve: 'smooth', width: 2 },
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
      type: 'gradient',
      gradient: {
        shade: 'light',
        type: 'vertical',
        opacityFrom: 0.6,
        opacityTo: 0.1,
      },
    },
    markers: { size: 0 },
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
    this.cryptoService.getMarketChart(this.coinId, this.selectedDays).subscribe({
      next: (data) => {
        this.updateChart(data);
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error fetching market chart:', error);
        this.error = 'Failed to load price chart data.';
        this.isLoading = false;
      },
    });
  }

  loadTechnicalAnalysis(): void {
    if (!this.coinData?.symbol) return;

    this.cryptoService.getTechnicalAnalysis(this.coinData.symbol).subscribe({
      next: (data) => {
        this.technicalAnalysis = data;

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
    const newOptions: Partial<ChartOptions> = {
      series: [
        {
          name: `${this.coinData.name} Price`,
          data: prices,
        },
      ],
      title: { text: `${this.coinData.name} Price Chart`, align: 'left' },
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
    };

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
    if (!data || !data.indicatorGroups) return;
    
    this.indicatorData = {};
    
    // If the backend provides time-series data for each indicator, use it
    if (data.indicatorSeries && Object.keys(data.indicatorSeries).length > 0) {
      // Process each time series
      Object.keys(data.indicatorSeries).forEach(key => {
        const series = data.indicatorSeries[key];
        if (series && series.length > 0) {
          // Convert to chart format
          this.indicatorData[key] = series.map((point: any) => ({
            x: new Date(point.timestamp),
            y: point.value
          }));
        }
      });
      
      // Update the chart with the indicator data
      this.updateChartWithIndicators();
      return;
    }
    
    // Fallback to using single points if time-series isn't available
    // Extract indicator data from groups
    data.indicatorGroups.forEach((group: any) => {
      if (group.type === 'sma') {
        const sma14 = group.indicators.find((i: any) => i.name === 'sma_14');
        if (sma14) {
          this.indicatorData['sma_14'] = [{ x: new Date(), y: sma14.value }];
        }
      }
      
      if (group.type === 'ema') {
        const ema14 = group.indicators.find((i: any) => i.name === 'ema_14');
        if (ema14) {
          this.indicatorData['ema_14'] = [{ x: new Date(), y: ema14.value }];
        }
      }
      
      if (group.type === 'bollinger') {
        const upper = group.indicators.find((i: any) => i.name === 'upper_band');
        const middle = group.indicators.find((i: any) => i.name === 'middle_band');
        const lower = group.indicators.find((i: any) => i.name === 'lower_band');
        
        if (upper) this.indicatorData['bollinger_upper'] = [{ x: new Date(), y: upper.value }];
        if (middle) this.indicatorData['bollinger_middle'] = [{ x: new Date(), y: middle.value }];
        if (lower) this.indicatorData['bollinger_lower'] = [{ x: new Date(), y: lower.value }];
      }
      
      if (group.type === 'rsi') {
        const rsi = group.indicators.find((i: any) => i.name === 'rsi');
        if (rsi) {
          this.indicatorData['rsi'] = [{ x: new Date(), y: rsi.value }];
        }
      }
      
      if (group.type === 'macd') {
        const macd = group.indicators.find((i: any) => i.name === 'macd_line');
        if (macd) {
          this.indicatorData['macd'] = [{ x: new Date(), y: macd.value }];
        }
      }
    });
    
    // Update chart with indicators if any are enabled
    this.updateChartWithIndicators();
  }

  toggleIndicator(indicator: string): void {
    this.enabledIndicators[indicator] = !this.enabledIndicators[indicator];
    this.updateChartWithIndicators();
  }

  toggleIndicatorPanel(): void {
    this.showIndicatorPanel = !this.showIndicatorPanel;
  }

  updateChartWithIndicators(): void {
    if (!this.chartOptions.series || this.chartOptions.series.length === 0) return;

    // Start with the price series
    const mainSeries = this.chartOptions.series[0];
    const newSeries = [mainSeries];

    // Add enabled indicator series
    Object.keys(this.enabledIndicators).forEach((indicator) => {
      if (this.enabledIndicators[indicator] && this.indicatorData[indicator]) {
        const seriesConfig: any = {
          name: this.formatIndicatorName(indicator),
          data: this.indicatorData[indicator],
          color: this.indicatorColors[indicator],
          type: 'line'
        };
        
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

    // Update chart series
    this.chartOptions.series = newSeries;
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
}
