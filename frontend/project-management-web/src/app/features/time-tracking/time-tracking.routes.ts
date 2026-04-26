import { Routes } from '@angular/router';

export const timeTrackingRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/time-entry-list/time-entry-list').then(m => m.TimeEntryListComponent),
  },
  {
    path: 'timesheet',
    loadComponent: () =>
      import('./components/timesheet-grid/timesheet-grid').then(m => m.TimesheetGridComponent),
  },
  {
    path: 'import',
    loadComponent: () =>
      import('./components/vendor-import/vendor-import').then(m => m.VendorImportComponent),
  },
  {
    path: 'period-lock',
    loadComponent: () =>
      import('./components/period-lock/period-lock').then(m => m.PeriodLockComponent),
  },
];
