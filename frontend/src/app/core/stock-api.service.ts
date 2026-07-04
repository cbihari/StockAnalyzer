import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AffiliateClickStat, AffiliatePartner, AiExplanationResponse, AiResearchResponse, CheckoutResponse, HistoricalPrice, IndicatorResponse, MarketOverview, MonetizationEventRequest, MonetizationEventResponse, MonetizationFunnelReport, MonetizationStatus, MlPrediction, ModelTrainingJob, ModelTrainingResult, ModelVersionsResponse, PortfolioHolding, PortfolioSummary, PredictionEvaluationResult, PredictionHistoryResponse, RazorpayOrderResponse, RazorpayPaymentVerificationRequest, RazorpayPaymentVerificationResponse, StockAnalysis, StockComparison, StockNews, StockQuotesResponse, StockSuggestion, TickerModelMetrics, WorkspaceAlertState, WorkspaceWatchlistItem } from './models';
import { ClientIdentityService } from './client-identity.service';

@Injectable({ providedIn: 'root' })
export class StockApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;
  private readonly identity = inject(ClientIdentityService);

  searchStocks(query: string): Observable<StockSuggestion[]> {
    return this.http.get<StockSuggestion[]>(`${this.baseUrl}/api/stocks/search`, {
      params: new HttpParams().set('query', query),
    });
  }

  getStockAnalysis(ticker: string, period = '1y'): Observable<StockAnalysis> {
    return this.http.get<StockAnalysis>(
      `${this.baseUrl}/api/stocks/${encodeURIComponent(ticker)}/analysis`,
      { params: new HttpParams().set('period', period) },
    );
  }

  compareStocks(tickers: string[], period = '1y'): Observable<StockComparison> {
    return this.http.get<StockComparison>(`${this.baseUrl}/api/stocks/compare`, {
      params: new HttpParams().set('tickers', tickers.join(',')).set('period', period),
    });
  }

  getMarketOverview(region: 'india' | 'us'): Observable<MarketOverview> {
    return this.http.get<MarketOverview>(`${this.baseUrl}/api/stocks/market/overview`, {
      params: new HttpParams().set('region', region),
    });
  }

  getStockQuotes(tickers: string[]): Observable<StockQuotesResponse> {
    return this.http.get<StockQuotesResponse>(`${this.baseUrl}/api/stocks/quotes`, {
      params: new HttpParams().set('tickers', tickers.join(',')),
    });
  }

  getStockNews(ticker: string, lookbackDays = 7, limit = 10): Observable<StockNews> {
    return this.http.get<StockNews>(
      `${this.baseUrl}/api/stocks/${encodeURIComponent(ticker)}/news`,
      { params: new HttpParams().set('lookbackDays', lookbackDays).set('limit', limit) },
    );
  }

  getHistory(ticker: string, period = '1y'): Observable<HistoricalPrice[]> {
    return this.http.get<HistoricalPrice[]>(
      `${this.baseUrl}/api/stocks/history/${encodeURIComponent(ticker)}`,
      { params: new HttpParams().set('period', period) },
    );
  }

  getIndicators(ticker: string, period = '1y'): Observable<IndicatorResponse> {
    return this.http.get<IndicatorResponse>(
      `${this.baseUrl}/api/stocks/indicators/${encodeURIComponent(ticker)}`,
      { params: new HttpParams().set('period', period) },
    );
  }

  getMlPrediction(ticker: string): Observable<MlPrediction> {
    return this.http.get<MlPrediction>(
      `${this.baseUrl}/api/predictions/ml/${encodeURIComponent(ticker)}`,
    );
  }

  generateAiExplanation(ticker: string, forceRefresh = false): Observable<AiExplanationResponse> {
    return this.http.post<AiExplanationResponse>(
      `${this.baseUrl}/api/predictions/explain-ai/${encodeURIComponent(ticker)}`,
      null,
      { params: new HttpParams().set('forceRefresh', forceRefresh) },
    );
  }

  askAiResearch(ticker: string, question: string): Observable<AiResearchResponse> {
    return this.http.post<AiResearchResponse>(
      `${this.baseUrl}/api/research/${encodeURIComponent(ticker)}`,
      { question },
    );
  }

  retrainModel(ticker: string, period = '5y'): Observable<ModelTrainingResult> {
    return this.http.post<ModelTrainingResult>(
      `${this.baseUrl}/api/model/train/${encodeURIComponent(ticker)}`,
      null,
      { params: new HttpParams().set('period', period) },
    );
  }

  getModelMetrics(ticker: string): Observable<TickerModelMetrics> {
    return this.http.get<TickerModelMetrics>(
      `${this.baseUrl}/api/model/metrics/${encodeURIComponent(ticker)}`,
    );
  }

  startTrainingJob(ticker: string, period = '5y'): Observable<ModelTrainingJob> {
    return this.http.post<ModelTrainingJob>(
      `${this.baseUrl}/api/model/train/${encodeURIComponent(ticker)}/jobs`,
      null,
      { params: new HttpParams().set('period', period) },
    );
  }

  getTrainingJob(jobId: string): Observable<ModelTrainingJob> {
    return this.http.get<ModelTrainingJob>(
      `${this.baseUrl}/api/model/train/jobs/${encodeURIComponent(jobId)}`,
    );
  }

  getModelVersions(ticker: string): Observable<ModelVersionsResponse> {
    return this.http.get<ModelVersionsResponse>(
      `${this.baseUrl}/api/model/versions/${encodeURIComponent(ticker)}`,
    );
  }

  getPredictionHistory(ticker = '', outcome = 'all', limit = 100): Observable<PredictionHistoryResponse> {
    let params = new HttpParams().set('outcome', outcome).set('limit', limit);
    if (ticker.trim()) params = params.set('ticker', ticker.trim());
    return this.http.get<PredictionHistoryResponse>(`${this.baseUrl}/api/predictions/history`, { params });
  }

  exportPredictionHistoryCsv(ticker = '', outcome = 'all', limit = 500): Observable<HttpResponse<Blob>> {
    let params = new HttpParams().set('outcome', outcome).set('limit', limit);
    if (ticker.trim()) params = params.set('ticker', ticker.trim());
    return this.http.get(`${this.baseUrl}/api/predictions/history/export`, {
      observe: 'response',
      params,
      responseType: 'blob',
    });
  }

  evaluatePredictions(): Observable<PredictionEvaluationResult> {
    return this.http.post<PredictionEvaluationResult>(`${this.baseUrl}/api/predictions/evaluate`, null);
  }

  getWorkspaceWatchlist(): Observable<WorkspaceWatchlistItem[]> {
    return this.http.get<WorkspaceWatchlistItem[]>(`${this.baseUrl}/api/workspace/watchlist`, { headers: this.workspaceHeaders() });
  }

  saveWorkspaceWatchlist(items: WorkspaceWatchlistItem[]): Observable<WorkspaceWatchlistItem[]> {
    return this.http.put<WorkspaceWatchlistItem[]>(`${this.baseUrl}/api/workspace/watchlist`, items, { headers: this.workspaceHeaders() });
  }

  getWorkspaceAlerts(): Observable<WorkspaceAlertState> {
    return this.http.get<WorkspaceAlertState>(`${this.baseUrl}/api/workspace/alerts`, { headers: this.workspaceHeaders() });
  }

  saveWorkspaceAlerts(state: WorkspaceAlertState): Observable<WorkspaceAlertState> {
    return this.http.put<WorkspaceAlertState>(`${this.baseUrl}/api/workspace/alerts`, state, { headers: this.workspaceHeaders() });
  }

  getWorkspacePortfolio(): Observable<PortfolioHolding[]> {
    return this.http.get<PortfolioHolding[]>(`${this.baseUrl}/api/workspace/portfolio`, { headers: this.workspaceHeaders() });
  }

  saveWorkspacePortfolio(holdings: PortfolioHolding[]): Observable<PortfolioHolding[]> {
    return this.http.put<PortfolioHolding[]>(`${this.baseUrl}/api/workspace/portfolio`, holdings, { headers: this.workspaceHeaders() });
  }

  getPortfolioSummary(): Observable<PortfolioSummary> {
    return this.http.get<PortfolioSummary>(`${this.baseUrl}/api/portfolio/summary`, { headers: this.workspaceHeaders() });
  }

  getAffiliatePartners(): Observable<AffiliatePartner[]> {
    return this.http.get<AffiliatePartner[]>(`${this.baseUrl}/api/affiliate/partners`);
  }

  trackAffiliateClick(broker: string, ticker = ''): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/api/affiliate/click`,
      { broker, ticker: ticker || null },
      { headers: this.workspaceHeaders() },
    );
  }

  getAffiliateStats(): Observable<AffiliateClickStat[]> {
    return this.http.get<AffiliateClickStat[]>(`${this.baseUrl}/api/affiliate/stats`);
  }

  getMonetizationStatus(): Observable<MonetizationStatus> {
    return this.http.get<MonetizationStatus>(`${this.baseUrl}/api/monetization/status`, {
      headers: this.workspaceHeaders(),
    });
  }

  startCheckout(planKey: 'pro' | 'power', successUrl: string, cancelUrl: string): Observable<CheckoutResponse> {
    return this.http.post<CheckoutResponse>(`${this.baseUrl}/api/monetization/checkout`, {
      planKey,
      successUrl,
      cancelUrl,
    });
  }

  createRazorpayOrder(planKey: 'pro' | 'power'): Observable<RazorpayOrderResponse> {
    return this.http.post<RazorpayOrderResponse>(`${this.baseUrl}/api/payments/create-order`, { planKey });
  }

  verifyRazorpayPayment(request: RazorpayPaymentVerificationRequest): Observable<RazorpayPaymentVerificationResponse> {
    return this.http.post<RazorpayPaymentVerificationResponse>(`${this.baseUrl}/api/payments/verify`, request);
  }

  recordMonetizationEvent(request: MonetizationEventRequest): Observable<MonetizationEventResponse> {
    return this.http.post<MonetizationEventResponse>(
      `${this.baseUrl}/api/monetization/events`,
      request,
      { headers: this.workspaceHeaders() },
    );
  }

  getMonetizationFunnel(days = 30): Observable<MonetizationFunnelReport> {
    return this.http.get<MonetizationFunnelReport>(`${this.baseUrl}/api/monetization/events/funnel`, {
      params: new HttpParams().set('days', days),
    });
  }

  exportMonetizationFunnelCsv(days = 30): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.baseUrl}/api/monetization/events/funnel/export`, {
      observe: 'response',
      params: new HttpParams().set('days', days),
      responseType: 'blob',
    });
  }

  private workspaceHeaders(): Record<string, string> { return { 'X-Client-ID': this.identity.id }; }
}
