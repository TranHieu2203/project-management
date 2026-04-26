import { Routes } from '@angular/router';

export const auditRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/audit-log/audit-log').then(m => m.AuditLogComponent),
  },
];
