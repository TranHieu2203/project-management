import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { createReducer, on } from '@ngrx/store';
import { Vendor } from '../models/vendor.model';
import { VendorsActions } from './vendors.actions';

export interface VendorConflictState {
  serverState: Vendor;
  eTag: string;
  pendingName: string;
  pendingDescription?: string;
}

export interface VendorsState extends EntityState<Vendor> {
  selectedId: string | null;
  loading: boolean;
  creating: boolean;
  updating: boolean;
  inactivating: boolean;
  error: string | null;
  conflict: VendorConflictState | null;
}

export const vendorsAdapter = createEntityAdapter<Vendor>();

export const initialVendorsState: VendorsState = vendorsAdapter.getInitialState({
  selectedId: null,
  loading: false,
  creating: false,
  updating: false,
  inactivating: false,
  error: null,
  conflict: null,
});

export const vendorsReducer = createReducer(
  initialVendorsState,

  on(VendorsActions.loadVendors, state => ({ ...state, loading: true, error: null })),
  on(VendorsActions.loadVendorsSuccess, (state, { vendors }) =>
    vendorsAdapter.setAll(vendors, { ...state, loading: false })
  ),
  on(VendorsActions.loadVendorsFailure, (state, { error }) => ({ ...state, loading: false, error })),
  on(VendorsActions.selectVendor, (state, { vendorId }) => ({ ...state, selectedId: vendorId })),

  on(VendorsActions.createVendor, state => ({ ...state, creating: true, error: null })),
  on(VendorsActions.createVendorSuccess, (state, { vendor }) =>
    vendorsAdapter.addOne(vendor, { ...state, creating: false })
  ),
  on(VendorsActions.createVendorFailure, (state, { error }) => ({ ...state, creating: false, error })),

  on(VendorsActions.updateVendor, state => ({ ...state, updating: true, error: null })),
  on(VendorsActions.updateVendorSuccess, (state, { vendor }) =>
    vendorsAdapter.updateOne({ id: vendor.id, changes: vendor }, { ...state, updating: false })
  ),
  on(VendorsActions.updateVendorFailure, (state, { error }) => ({ ...state, updating: false, error })),
  on(VendorsActions.updateVendorConflict, (state, { serverState, eTag, pendingName, pendingDescription }) => ({
    ...state,
    updating: false,
    conflict: { serverState, eTag, pendingName, pendingDescription },
  })),

  on(VendorsActions.inactivateVendor, state => ({ ...state, inactivating: true, error: null })),
  on(VendorsActions.inactivateVendorSuccess, (state, { vendor }) =>
    vendorsAdapter.updateOne({ id: vendor.id, changes: vendor }, { ...state, inactivating: false })
  ),
  on(VendorsActions.inactivateVendorFailure, (state, { error }) => ({ ...state, inactivating: false, error })),

  on(VendorsActions.clearConflict, state => ({ ...state, conflict: null })),
);
