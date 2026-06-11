export interface HistoricalPrice {
  date: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

export interface IndicatorValues {
  date: string;
  daily_return: number | null;
  SMA_20: number | null;
  SMA_50: number | null;
  EMA_20: number | null;
  EMA_50: number | null;
  RSI_14: number | null;
  MACD: number | null;
  MACD_signal: number | null;
  bollinger_upper: number | null;
  bollinger_lower: number | null;
  volume_change: number | null;
}

export interface IndicatorResponse {
  ticker: string;
  period: string;
  latest: IndicatorValues;
}

export interface MlPrediction {
  ticker: string;
  prediction: 'UP' | 'DOWN';
  confidence: number;
  probability_up: number;
  probability_down: number;
  model: string;
  latest_close: number | null;
  reasons: string[];
  model_status: 'existing_model' | 'newly_trained_model' | 'rule_based_fallback';
  model_accuracy: number | null;
  warning: string;
  model_trained?: boolean;
  fallback_used?: boolean;
  prediction_type: 'ml' | 'rule_based_fallback';
  reason: string | null;
  technical_reasons: string[];
}

export interface ModelTrainingResult {
  ticker: string;
  status: 'trained';
  accuracy: number;
  precision: number;
  recall: number;
  model_path: string;
  trained_at: string;
}

export interface TickerModelMetrics {
  ticker: string;
  model_status: 'trained';
  model_name: string;
  trained_at: string;
  accuracy: number;
  precision: number;
  recall: number;
  confusion_matrix: number[][];
  training_rows: number;
  testing_rows: number;
}

export interface PredictionHistoryItem extends MlPrediction {
  createdAt: string;
}
