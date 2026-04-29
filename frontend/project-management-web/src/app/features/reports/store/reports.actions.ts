import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { BudgetReport, ReportsFilters } from '../models/budget-report.model';

export const ReportsActions = createActionGroup({
  source: 'Reports',
  events: {
    'Set Filters': props<{ filters: ReportsFilters }>(),
    'Load Budget Report': emptyProps(),
    'Load Budget Report Success': props<{ report: BudgetReport }>(),
    'Load Budget Report Failure': props<{ error: string }>(),
  },
});
