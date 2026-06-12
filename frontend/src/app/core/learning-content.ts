export interface LearningLesson {
  slug: string;
  category: string;
  title: string;
  duration: string;
  summary: string;
  definition: string;
  matters: string;
  interpretation: { label: string; detail: string }[];
  mistake: string;
  example: string;
  takeaway: string;
  quiz: { question: string; options: string[]; answer: number; explanation: string };
}

export const LEARNING_LESSONS: LearningLesson[] = [
  {
    slug: 'rsi', category: 'Momentum', title: 'RSI: momentum without the myth', duration: '4 min',
    summary: 'Read the 0–100 momentum scale without treating overbought or oversold as automatic trade signals.',
    definition: 'Relative Strength Index compares the size of recent gains with recent losses, usually across 14 sessions.',
    matters: 'RSI helps show whether recent buying or selling pressure is unusually strong, but price can remain extreme for a long time.',
    interpretation: [{ label: 'Below 30', detail: 'Selling momentum is stretched. This is context for a possible recovery, not proof of one.' }, { label: '30–70', detail: 'Momentum is broadly neutral; trend and volume usually deserve more weight.' }, { label: 'Above 70', detail: 'Buying momentum is stretched. Strong trends can remain above 70.' }],
    mistake: 'Buying only because RSI is below 30 or selling only because it is above 70.',
    example: 'If RSI rises from 28 to 38 while price stabilizes and volume improves, momentum is recovering. RSI at 28 alone is weaker evidence.',
    takeaway: 'Use RSI as momentum context and combine it with trend, price structure, and participation.',
    quiz: { question: 'What does RSI above 70 prove?', options: ['The stock must fall tomorrow', 'Momentum is stretched, but direction is not guaranteed', 'The company is overvalued'], answer: 1, explanation: 'RSI measures price momentum, not valuation, and extreme readings can persist.' },
  },
  {
    slug: 'macd', category: 'Momentum', title: 'MACD: trend and momentum together', duration: '5 min',
    summary: 'Understand the MACD line, signal line, crossovers, and why the indicator naturally lags price.',
    definition: 'MACD measures the difference between two exponential moving averages. A signal-line average helps identify momentum changes.',
    matters: 'The relationship between MACD and its signal line can reveal acceleration or deceleration inside a broader trend.',
    interpretation: [{ label: 'MACD above signal', detail: 'Momentum is improving relative to its recent path.' }, { label: 'MACD below signal', detail: 'Momentum is weakening relative to its recent path.' }, { label: 'Near zero', detail: 'The faster and slower trend estimates are close together.' }],
    mistake: 'Treating every crossover as an immediate reversal while ignoring sideways-market noise.',
    example: 'A bullish crossover above zero with rising volume is stronger context than a tiny crossover around zero on low volume.',
    takeaway: 'Crossovers describe changing momentum. Confirm them with trend, volume, and price structure.',
    quiz: { question: 'Why can MACD react late?', options: ['It uses averaged historical prices', 'It reads company earnings', 'It predicts future volume'], answer: 0, explanation: 'Moving averages smooth past prices, so they respond after price begins moving.' },
  },
  {
    slug: 'ema', category: 'Trend', title: 'EMA: a faster view of trend', duration: '4 min',
    summary: 'See how exponential averages emphasize recent prices and how EMA20/EMA50 alignment describes trend.',
    definition: 'An Exponential Moving Average gives more weight to recent observations than older ones.',
    matters: 'EMA reacts faster than a simple average, making it useful for observing shorter-term trend changes.',
    interpretation: [{ label: 'EMA20 above EMA50', detail: 'Recent prices are stronger than the medium-term baseline.' }, { label: 'EMA20 below EMA50', detail: 'Recent prices are weaker than the medium-term baseline.' }, { label: 'Narrow gap', detail: 'Trend conviction may be limited or transitioning.' }],
    mistake: 'Assuming one crossover guarantees a durable new trend.',
    example: 'EMA20 above EMA50, both rising, is better trend evidence than two flat averages repeatedly crossing.',
    takeaway: 'Read direction, slope, and separation together rather than using crossover alone.',
    quiz: { question: 'What makes EMA more responsive than SMA?', options: ['It uses future data', 'It weights recent prices more heavily', 'It excludes down days'], answer: 1, explanation: 'EMA assigns greater influence to newer observations.' },
  },
  {
    slug: 'sma', category: 'Trend', title: 'SMA: smoothing market noise', duration: '3 min',
    summary: 'Learn how a simple average creates a baseline for comparing current price with recent history.',
    definition: 'A Simple Moving Average is the arithmetic mean of closing prices over a fixed number of sessions.',
    matters: 'SMA smooths daily noise and makes medium-term direction easier to see.',
    interpretation: [{ label: 'Price above SMA', detail: 'Current price is stronger than its recent average.' }, { label: 'Price below SMA', detail: 'Current price is weaker than its recent average.' }, { label: 'Rising SMA', detail: 'The historical baseline itself is trending upward.' }],
    mistake: 'Using SMA as an exact support or resistance price rather than a changing reference zone.',
    example: 'Price above a rising SMA50 suggests healthier structure than price briefly crossing a flat SMA50.',
    takeaway: 'SMA is a trend baseline, not a prediction engine.',
    quiz: { question: 'What does SMA20 calculate?', options: ['The highest close in 20 days', 'The average close over 20 sessions', 'Tomorrow’s expected close'], answer: 1, explanation: 'SMA is a backward-looking arithmetic average.' },
  },
  {
    slug: 'volume', category: 'Participation', title: 'Volume: who is participating?', duration: '4 min',
    summary: 'Use volume to judge whether market participation supports or questions a price move.',
    definition: 'Volume is the number of shares traded during a session. Volume change compares participation with the previous session.',
    matters: 'Price movement accompanied by expanding volume often has broader participation than the same move on shrinking volume.',
    interpretation: [{ label: 'Price up, volume up', detail: 'Participation supports the advance.' }, { label: 'Price up, volume down', detail: 'The move may have weaker confirmation.' }, { label: 'Price down, volume up', detail: 'Selling participation is expanding and risk may be elevated.' }],
    mistake: 'Calling all high volume bullish. Volume confirms participation, not direction.',
    example: 'A breakout above resistance with volume 60% above the prior session has more confirmation than a low-volume drift above it.',
    takeaway: 'Always read volume beside price direction and recent norms.',
    quiz: { question: 'What does rising volume tell you by itself?', options: ['The stock will rise', 'More trading participation occurred', 'The company is profitable'], answer: 1, explanation: 'Volume shows activity. Price direction determines whether that activity accompanied buying or selling pressure.' },
  },
  {
    slug: 'risk', category: 'Risk', title: 'Risk management: uncertainty first', duration: '5 min',
    summary: 'Understand volatility, drawdown, diversification, and why confidence is not the probability of profit.',
    definition: 'Risk management is the practice of recognizing uncertain outcomes and limiting how much one adverse outcome can matter.',
    matters: 'A direction estimate can be correct and still produce a poor result if volatility, timing, costs, or concentration are ignored.',
    interpretation: [{ label: 'Volatility', detail: 'How widely returns have varied, not whether they are good or bad.' }, { label: 'Drawdown', detail: 'The decline from a prior peak to a later low.' }, { label: 'Diversification', detail: 'Reducing dependence on a single stock, sector, or outcome.' }],
    mistake: 'Treating model confidence or historical accuracy as a guarantee or personalized position-size recommendation.',
    example: 'A 70% confidence estimate in a highly volatile stock can carry more uncertainty than a lower-confidence estimate in a stable market.',
    takeaway: 'Direction, confidence, volatility, and personal suitability are different questions.',
    quiz: { question: 'Model confidence is best understood as:', options: ['Guaranteed return probability', 'The model’s certainty for this classification', 'A recommended portfolio allocation'], answer: 1, explanation: 'Confidence describes a model output, not profit, suitability, or position size.' },
  },
];

export const LEARNING_GLOSSARY = [
  ['Confidence', 'How strongly the model favors its predicted class for one observation.'],
  ['Model accuracy', 'The share of correct classifications in a historical holdout test.'],
  ['Support', 'A recent price area where declines have previously slowed.'],
  ['Resistance', 'A recent price area where advances have previously slowed.'],
  ['Volatility', 'The degree to which returns vary over time.'],
  ['OHLCV', 'Open, high, low, close, and volume market data.'],
] as const;

export function findLesson(slug: string | null): LearningLesson | undefined {
  return LEARNING_LESSONS.find((lesson) => lesson.slug === slug);
}
