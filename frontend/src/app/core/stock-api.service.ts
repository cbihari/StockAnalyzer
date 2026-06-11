import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { HistoricalPrice, IndicatorResponse, MlPrediction, ModelTrainingResult, TickerModelMetrics } from './models';

@Injectable({ providedIn: 'root' })
export class StockApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

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
}
