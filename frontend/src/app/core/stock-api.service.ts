import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { HistoricalPrice, IndicatorResponse, MarketOverview, MlPrediction, ModelTrainingJob, ModelTrainingResult, ModelVersionsResponse, PredictionEvaluationResult, PredictionHistoryResponse, StockAnalysis, StockComparison, StockQuotesResponse, StockSuggestion, TickerModelMetrics } from './models';

@Injectable({ providedIn: 'root' })
export class StockApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

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

  evaluatePredictions(): Observable<PredictionEvaluationResult> {
    return this.http.post<PredictionEvaluationResult>(`${this.baseUrl}/api/predictions/evaluate`, null);
  }
}
