import { createSelector } from '@ngrx/store';
import { AppState } from '../../../core/store/app.state';
import { vendorsAdapter, VendorsState } from './vendors.reducer';

const selectVendorsState = (state: AppState) => state.vendors;

const { selectAll, selectEntities } = vendorsAdapter.getSelectors(selectVendorsState);

export const selectAllVendors = selectAll;
export const selectVendorEntities = selectEntities;
export const selectVendorsLoading = createSelector(selectVendorsState, (s: VendorsState) => s.loading);
export const selectVendorsCreating = createSelector(selectVendorsState, (s: VendorsState) => s.creating);
export const selectVendorsUpdating = createSelector(selectVendorsState, (s: VendorsState) => s.updating);
export const selectVendorsInactivating = createSelector(selectVendorsState, (s: VendorsState) => s.inactivating);
export const selectVendorConflict = createSelector(selectVendorsState, (s: VendorsState) => s.conflict);
export const selectVendorsError = createSelector(selectVendorsState, (s: VendorsState) => s.error);
export const selectSelectedVendorId = createSelector(selectVendorsState, (s: VendorsState) => s.selectedId);
export const selectSelectedVendor = createSelector(
  selectVendorEntities,
  selectSelectedVendorId,
  (entities, id) => (id ? entities[id] : null)
);
