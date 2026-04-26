import { createSelector } from '@ngrx/store';
import { AppState } from '../../../core/store/app.state';
import { ratesAdapter, RatesState } from './rates.reducer';

const selectRatesState = (state: AppState) => state.rates;

const { selectAll, selectEntities } = ratesAdapter.getSelectors(selectRatesState);

export const selectAllRates = selectAll;
export const selectRateEntities = selectEntities;
export const selectRatesLoading = createSelector(selectRatesState, (s: RatesState) => s.loading);
export const selectRatesCreating = createSelector(selectRatesState, (s: RatesState) => s.creating);
export const selectRatesDeleting = createSelector(selectRatesState, (s: RatesState) => s.deleting);
export const selectRatesError = createSelector(selectRatesState, (s: RatesState) => s.error);
