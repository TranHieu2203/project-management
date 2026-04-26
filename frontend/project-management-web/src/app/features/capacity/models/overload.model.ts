export interface OverloadDayResult {
  date: string;
  hours: number;
  isOverloaded: boolean;
}

export interface OverloadWeekResult {
  weekStart: string;
  totalHours: number;
  isOverloaded: boolean;
  days: OverloadDayResult[];
}

export interface ResourceOverloadResult {
  resourceId: string;
  dailyBreakdown: OverloadDayResult[];
  weeklyBreakdown: OverloadWeekResult[];
  hasOverload: boolean;
}
