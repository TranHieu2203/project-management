import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { MonthlyRate } from '../models/monthly-rate.model';

export const RatesActions = createActionGroup({
  source: 'Rates',
  events: {
    'Load Rates': props<{ vendorId?: string; year?: number; month?: number }>(),
    'Load Rates Success': props<{ rates: MonthlyRate[] }>(),
    'Load Rates Failure': props<{ error: string }>(),

    'Create Rate': props<{ vendorId: string; role: string; level: string; year: number; month: number; monthlyAmount: number }>(),
    'Create Rate Success': props<{ rate: MonthlyRate }>(),
    'Create Rate Failure': props<{ error: string }>(),

    'Delete Rate': props<{ rateId: string }>(),
    'Delete Rate Success': props<{ rateId: string }>(),
    'Delete Rate Failure': props<{ error: string }>(),
  },
});
