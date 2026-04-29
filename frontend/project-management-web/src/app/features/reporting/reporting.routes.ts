import { Routes } from '@angular/router';

export const reportingRoutes: Routes = [
  {
    path: 'cost',
    loadComponent: () =>
      import('./components/cost-dashboard/cost-dashboard').then(m => m.CostDashboardComponent),
  },
  {
    path: 'breakdown',
    loadComponent: () =>
      import('./components/cost-breakdown/cost-breakdown').then(m => m.CostBreakdownComponent),
  },
  {
    path: 'export',
    loadComponent: () =>
      import('./components/export-trigger/export-trigger').then(m => m.ExportTriggerComponent),
  },
  {
    path: 'resources',
    loadComponent: () =>
      import('./components/resource-report/resource-report').then(m => m.ResourceReportComponent),
  },
  {
    path: 'milestones',
    loadComponent: () =>
      import('./components/milestone-report/milestone-report').then(m => m.MilestoneReportComponent),
  },
  { path: '', redirectTo: 'cost', pathMatch: 'full' },
];
