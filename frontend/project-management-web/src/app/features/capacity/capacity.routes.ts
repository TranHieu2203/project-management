import { Routes } from '@angular/router';

export const capacityRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/overload-dashboard/overload-dashboard').then(m => m.OverloadDashboardComponent),
  },
  {
    path: 'cross-project',
    loadComponent: () =>
      import('./components/cross-project-aggregation/cross-project-aggregation').then(m => m.CrossProjectAggregationComponent),
  },
  {
    path: 'heatmap',
    loadComponent: () =>
      import('./components/capacity-heatmap/capacity-heatmap').then(m => m.CapacityHeatmapComponent),
  },
  {
    path: 'forecast',
    loadComponent: () =>
      import('./components/forecast-view/forecast-view').then(m => m.ForecastViewComponent),
  },
];
