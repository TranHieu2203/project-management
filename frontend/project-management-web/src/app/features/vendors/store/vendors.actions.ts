import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { Vendor } from '../models/vendor.model';

export const VendorsActions = createActionGroup({
  source: 'Vendors',
  events: {
    'Load Vendors': props<{ activeOnly?: boolean }>(),
    'Load Vendors Success': props<{ vendors: Vendor[] }>(),
    'Load Vendors Failure': props<{ error: string }>(),
    'Select Vendor': props<{ vendorId: string | null }>(),

    'Create Vendor': props<{ code: string; name: string; description?: string }>(),
    'Create Vendor Success': props<{ vendor: Vendor }>(),
    'Create Vendor Failure': props<{ error: string }>(),

    'Update Vendor': props<{ vendorId: string; name: string; description?: string; version: number }>(),
    'Update Vendor Success': props<{ vendor: Vendor }>(),
    'Update Vendor Failure': props<{ error: string }>(),
    'Update Vendor Conflict': props<{ serverState: Vendor; eTag: string; pendingName: string; pendingDescription?: string }>(),

    'Inactivate Vendor': props<{ vendorId: string; version: number }>(),
    'Inactivate Vendor Success': props<{ vendor: Vendor }>(),
    'Inactivate Vendor Failure': props<{ error: string }>(),

    'Clear Conflict': emptyProps(),
  },
});
