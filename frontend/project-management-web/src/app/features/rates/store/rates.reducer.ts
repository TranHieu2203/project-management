import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { createReducer, on } from '@ngrx/store';
import { MonthlyRate } from '../models/monthly-rate.model';
import { RatesActions } from './rates.actions';

export interface RatesState extends EntityState<MonthlyRate> {
  loading: boolean;
  creating: boolean;
  deleting: boolean;
  error: string | null;
}

export const ratesAdapter = createEntityAdapter<MonthlyRate>();

export const initialRatesState: RatesState = ratesAdapter.getInitialState({
  loading: false,
  creating: false,
  deleting: false,
  error: null,
});

export const ratesReducer = createReducer(
  initialRatesState,

  on(RatesActions.loadRates, state => ({ ...state, loading: true, error: null })),
  on(RatesActions.loadRatesSuccess, (state, { rates }) =>
    ratesAdapter.setAll(rates, { ...state, loading: false })
  ),
  on(RatesActions.loadRatesFailure, (state, { error }) => ({ ...state, loading: false, error })),

  on(RatesActions.createRate, state => ({ ...state, creating: true, error: null })),
  on(RatesActions.createRateSuccess, (state, { rate }) =>
    ratesAdapter.addOne(rate, { ...state, creating: false })
  ),
  on(RatesActions.createRateFailure, (state, { error }) => ({ ...state, creating: false, error })),

  on(RatesActions.deleteRate, state => ({ ...state, deleting: true, error: null })),
  on(RatesActions.deleteRateSuccess, (state, { rateId }) =>
    ratesAdapter.removeOne(rateId, { ...state, deleting: false })
  ),
  on(RatesActions.deleteRateFailure, (state, { error }) => ({ ...state, deleting: false, error })),
);
