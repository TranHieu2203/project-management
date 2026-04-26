import { Routes } from '@angular/router';

export const ratesRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/rate-list/rate-list').then(m => m.RateListComponent),
  },
];
