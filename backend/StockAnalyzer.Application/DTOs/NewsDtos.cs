using System.Text.Json.Serialization;

namespace StockAnalyzer.Application.DTOs;

public sealed record NewsArticleDto(
    string Id,
    string Headline,
    string Publisher,
    [property: JsonPropertyName("published_at")] DateTimeOffset PublishedAt,
    string Url,
    string Sentiment,
    [property: JsonPropertyName("sentiment_score")] double SentimentScore,
    string Impact,
    string Topic,
    string Summary,
    [property: JsonPropertyName("why_it_matters")] string WhyItMatters);

public sealed record StockNewsDto(
    string Ticker,
    [property: JsonPropertyName("overall_sentiment")] string OverallSentiment,
    [property: JsonPropertyName("sentiment_score")] double SentimentScore,
    double Confidence,
    string Coverage,
    [property: JsonPropertyName("article_count")] int ArticleCount,
    [property: JsonPropertyName("lookback_days")] int LookbackDays,
    [property: JsonPropertyName("highest_impact_topic")] string? HighestImpactTopic,
    [property: JsonPropertyName("positive_count")] int PositiveCount,
    [property: JsonPropertyName("neutral_count")] int NeutralCount,
    [property: JsonPropertyName("negative_count")] int NegativeCount,
    IReadOnlyList<NewsArticleDto> Articles,
    [property: JsonPropertyName("as_of")] DateTimeOffset AsOf,
    [property: JsonPropertyName("data_source")] string DataSource,
    string Methodology,
    string Warning);
