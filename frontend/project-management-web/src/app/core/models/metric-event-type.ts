export const MetricEventType = {
  PredictiveOverride: 'predictive_override',
  ForecastProactive: 'forecast_proactive',
  SuggestionAccept: 'suggestion_accept',
  SuggestionOverride: 'suggestion_override',
} as const;

export type MetricEventType = (typeof MetricEventType)[keyof typeof MetricEventType];
