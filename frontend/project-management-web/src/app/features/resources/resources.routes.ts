import { Routes } from '@angular/router';

export const resourcesRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/resource-list/resource-list').then(m => m.ResourceListComponent),
  },
];
