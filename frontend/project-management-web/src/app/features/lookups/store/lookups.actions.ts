import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { LookupItem } from '../models/lookup.model';

export const LookupsActions = createActionGroup({
  source: 'Lookups',
  events: {
    'Load Catalog': emptyProps(),
    'Load Catalog Success': props<{ roles: LookupItem[]; levels: LookupItem[] }>(),
    'Load Catalog Failure': props<{ error: string }>(),
  },
});
