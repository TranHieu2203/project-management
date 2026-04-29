import { Routes } from '@angular/router';
import { provideState } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { authGuard } from '../../core/auth/auth.guard';
import { reportsFeature } from './store/reports.reducer';
import { ReportsEffects } from './store/reports.effects';

export const REPORTS_ROUTES: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    providers: [
      provideState(reportsFeature),
      provideEffects([ReportsEffects]),
    ],
    loadComponent: () =>
      import('./shells/report-shell/report-shell').then(m => m.ReportShellComponent),
    children: [
      {
        path: 'budget',
        loadComponent: () =>
          import('./components/budget/budget-report/budget-report').then(m => m.BudgetReportComponent),
      },
      {
        path: '',
        redirectTo: 'budget',
        pathMatch: 'full',
      },
    ],
  },
];
