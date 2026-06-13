export interface HistoricalPrice {
  date: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

export interface StockSuggestion {
  symbol: string;
  name: string;
  exchange: string;
  type: string;
  country: string;
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

export interface ModelTrainingJob {
  job_id: string;
  ticker: string;
  period: string;
  status: 'queued' | 'running' | 'succeeded' | 'failed';
  submitted_at: string;
  started_at: string | null;
  completed_at: string | null;
  error: string | null;
  accuracy: number | null;
  precision: number | null;
  recall: number | null;
  model_path: string | null;
  trained_at: string | null;
}

export interface ModelVersion {
  version_id: string;
  ticker: string;
  model_name: string;
  model_path: string;
  metrics_path: string;
  trained_at: string;
  training_rows: number;
  test_rows: number;
  accuracy: number;
  precision: number;
  recall: number;
  features: string[];
  confusion_matrix: number[][];
  feature_importance: Record<string, number>;
  is_active: boolean;
}

export interface ModelVersionsResponse {
  ticker: string;
  versions: ModelVersion[];
}

export interface AnalysisSignal {
  category: string;
  label: string;
  detail: string;
  direction: 'UP' | 'DOWN' | 'NEUTRAL';
}

export interface StockAnalysis {
  ticker: string;
  quote: {
    ticker: string;
    latestPrice: number;
    previousClose: number;
    daily_change: number;
    daily_change_percent: number;
    as_of: string;
    currency: string;
  };
  prediction: MlPrediction;
  indicators: IndicatorResponse;
  history: HistoricalPrice[];
  supportingSignals: AnalysisSignal[];
  conflictingSignals: AnalysisSignal[];
  risk: { level: 'LOW' | 'MEDIUM' | 'HIGH'; score: number; factors: string[]; summary: string };
  marketContext: {
    support: number;
    resistance: number;
    rangePosition: number;
    annualizedVolatility: number;
    averageDailyRange: number;
    invalidation: string;
    lookbackSessions: number;
  };
  trend: 'BULLISH' | 'BEARISH' | 'DATA_LIMITED';
  volume_state: 'EXPANDING' | 'CONTRACTING' | 'STABLE' | 'DATA_LIMITED';
  data_source: string;
  generated_at: string;
  disclaimer: string;
}

export interface StockComparison {
  period: string;
  stocks: StockAnalysis[];
  generated_at: string;
  disclaimer: string;
}

export interface MarketInstrument {
  symbol: string;
  name: string;
  price: number;
  change: number;
  change_percent: number;
  day_high: number;
  day_low: number;
  volume: number;
  sparkline: number[];
}

export interface MarketOverview {
  region: 'india' | 'us';
  session_status: string;
  as_of: string;
  data_source: string;
  coverage_note: string;
  indices: MarketInstrument[];
  breadth: { advancers: number; decliners: number; unchanged: number; sentiment: 'POSITIVE' | 'MIXED' | 'NEGATIVE'; coverage: number };
  top_gainers: MarketInstrument[];
  top_losers: MarketInstrument[];
  most_active: MarketInstrument[];
  insights: string[];
}

export interface StockQuotesResponse {
  as_of: string;
  data_source: string;
  quotes: MarketInstrument[];
}

export interface NewsArticle {
  id: string;
  headline: string;
  publisher: string;
  published_at: string;
  url: string;
  sentiment: 'POSITIVE' | 'NEUTRAL' | 'NEGATIVE';
  sentiment_score: number;
  impact: 'HIGH' | 'MEDIUM' | 'LOW';
  topic: string;
  summary: string;
  why_it_matters: string;
}

export interface StockNews {
  ticker: string;
  overall_sentiment: 'POSITIVE' | 'NEUTRAL' | 'NEGATIVE';
  sentiment_score: number;
  confidence: number;
  coverage: 'HIGH' | 'MEDIUM' | 'LOW' | 'NONE';
  article_count: number;
  lookback_days: number;
  highest_impact_topic: string | null;
  positive_count: number;
  neutral_count: number;
  negative_count: number;
  articles: NewsArticle[];
  as_of: string;
  data_source: string;
  methodology: string;
  warning: string;
}

export interface AiExplanationSignal {
  signal: string;
  explanation: string;
  importance: 'low' | 'medium' | 'high';
}

export interface StockAiExplanation {
  ticker: string;
  prediction: 'UP' | 'DOWN';
  confidence: number;
  summary: string;
  supporting_signals: AiExplanationSignal[];
  conflicting_signals: AiExplanationSignal[];
  risk_level: 'LOW' | 'MEDIUM' | 'HIGH';
  risk_factors: string[];
  what_could_change_the_view: string[];
  beginner_explanation: string;
  data_limitations: string[];
  disclaimer: string;
}

export interface AiExplanationResponse {
  ticker: string;
  explanation: StockAiExplanation;
  provider: 'openai' | 'deterministic';
  model: string;
  fallbackUsed: boolean;
  fallbackReason: string | null;
  generatedAt: string;
  cached: boolean;
}

export interface AiResearchCitation {
  source: 'prediction' | 'technical_indicators' | 'market_context' | 'news_sentiment' | 'model_performance';
  label: string;
  evidence: string;
  observed_at: string;
}

export interface StockResearchAnswer {
  ticker: string;
  question: string;
  answer: string;
  key_points: string[];
  citations: AiResearchCitation[];
  limitations: string[];
  follow_up_questions: string[];
  disclaimer: string;
}

export interface AiResearchResponse {
  ticker: string;
  answer: StockResearchAnswer;
  provider: 'openai' | 'deterministic';
  model: string;
  fallbackUsed: boolean;
  fallbackReason: string | null;
  generatedAt: string;
  cached: boolean;
}

export interface WorkspaceWatchlistItem {
  ticker: string;
  addedAt: string;
  note: string;
  tags: string[];
}

export interface WorkspaceAlertRule {
  id: string;
  ticker: string;
  type: 'price_above' | 'price_below' | 'daily_move';
  threshold: number;
  frequency: 'once' | 'daily';
  cooldownHours: number;
  quietStart: string;
  quietEnd: string;
  enabled: boolean;
  createdAt: string;
  lastTriggeredAt: string | null;
}

export interface WorkspaceNotification {
  id: string;
  alertId: string;
  ticker: string;
  title: string;
  message: string;
  triggeredAt: string;
  dataTimestamp: string;
  evidenceUrl: string;
  read: boolean;
}

export interface WorkspaceAlertState {
  rules: WorkspaceAlertRule[];
  notifications: WorkspaceNotification[];
}

export interface PortfolioHolding {
  id: string;
  ticker: string;
  quantity: number;
  average_cost: number;
  purchased_at: string | null;
  note: string;
}

export interface PortfolioHoldingSummary extends PortfolioHolding {
  currency: 'INR' | 'USD';
  current_price: number;
  cost_basis: number;
  market_value: number;
  unrealized_gain: number;
  gain_percent: number;
  day_change_value: number;
  weight_percent: number;
}

export interface PortfolioCurrencyBucket {
  currency: 'INR' | 'USD';
  cost_basis: number;
  market_value: number;
  unrealized_gain: number;
  gain_percent: number;
  day_change_value: number;
  holding_count: number;
}

export interface PortfolioSummary {
  holdings: PortfolioHoldingSummary[];
  buckets: PortfolioCurrencyBucket[];
  risk_flags: string[];
  missing_tickers: string[];
  as_of: string;
  data_source: string;
  disclaimer: string;
}

export interface PredictionHistoryItem extends MlPrediction {
  createdAt: string;
}

export interface PersistedPredictionHistoryItem {
  id: string;
  ticker: string;
  prediction: 'UP' | 'DOWN';
  confidence: number;
  probability_up: number | null;
  probability_down: number | null;
  prediction_type: string;
  model_status: string | null;
  model_accuracy: number | null;
  created_at: string;
  actual_result: 'UP' | 'DOWN' | null;
  is_correct: boolean | null;
}

export interface PredictionHistoryResponse {
  items: PersistedPredictionHistoryItem[];
  total: number;
  evaluated: number;
  pending: number;
  correct: number;
  wrong: number;
  accuracy_percentage: number;
}

export interface PredictionEvaluationResult {
  evaluatedPredictions: number;
  pendingPredictions: number;
}

export interface AffiliatePartner {
  name: string;
  url: string;
  logo: string;
}

export interface AffiliateClickStat {
  broker: string;
  date: string;
  clicks: number;
}

export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  createdAt: string;
}

export interface AuthResponse {
  token: string;
  user: AuthUser;
}
