namespace ProjectManagement.Metrics.Domain.Enums;

public static class MetricEventType
{
    public const string PredictiveOverride = "predictive_override";
    public const string ForecastProactive  = "forecast_proactive";
    public const string SuggestionAccept   = "suggestion_accept";
    public const string SuggestionOverride = "suggestion_override";

    private static readonly HashSet<string> _valid =
    [
        PredictiveOverride, ForecastProactive,
        SuggestionAccept, SuggestionOverride
    ];

    public static bool IsValid(string type) => _valid.Contains(type);
}
