import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/components/login/login').then(m => m.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./core/shell/app-shell').then(m => m.AppShellComponent),
    children: [
      {
        path: 'my-tasks',
        loadComponent: () =>
          import('./features/projects/components/my-tasks/my-tasks').then(
            m => m.MyTasksComponent
          ),
      },
      {
        path: 'projects',
        loadChildren: () =>
          import('./features/projects/projects.routes').then(m => m.projectsRoutes),
      },
      {
        path: 'vendors',
        loadChildren: () =>
          import('./features/vendors/vendors.routes').then(m => m.vendorsRoutes),
      },
      {
        path: 'resources',
        loadChildren: () =>
          import('./features/resources/resources.routes').then(m => m.resourcesRoutes),
      },
      {
        path: 'rates',
        loadChildren: () =>
          import('./features/rates/rates.routes').then(m => m.ratesRoutes),
      },
      {
        path: 'audit',
        loadChildren: () =>
          import('./features/audit/audit.routes').then(m => m.auditRoutes),
      },
      {
        path: 'time-tracking',
        loadChildren: () =>
          import('./features/time-tracking/time-tracking.routes').then(m => m.timeTrackingRoutes),
      },
      {
        path: 'capacity',
        loadChildren: () =>
          import('./features/capacity/capacity.routes').then(m => m.capacityRoutes),
      },
      {
        path: 'reporting',
        loadChildren: () =>
          import('./features/reporting/reporting.routes').then(m => m.reportingRoutes),
      },
      {
        path: 'settings/notifications',
        loadComponent: () =>
          import('./features/settings/notification-preferences/notification-preferences')
            .then(m => m.NotificationPreferencesComponent),
      },
      { path: '', redirectTo: 'projects', pathMatch: 'full' },
    ],
  },
  { path: '**', redirectTo: 'login' },
];
