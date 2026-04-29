import { Routes } from '@angular/router';
import { DashboardShellComponent } from './shells/dashboard-shell/dashboard-shell';

export const dashboardRoutes: Routes = [
  {
    path: '',
    component: DashboardShellComponent,
    children: [
      {
        path: 'overview',
        loadComponent: () =>
          import('./components/overview/dashboard-overview').then(
            m => m.DashboardOverviewComponent
          ),
      },
      {
        path: 'my-tasks',
        loadComponent: () =>
          import('./components/my-tasks/my-tasks').then(
            m => m.DashboardMyTasksComponent
          ),
      },
      { path: '', redirectTo: 'overview', pathMatch: 'full' },
    ],
  },
];
