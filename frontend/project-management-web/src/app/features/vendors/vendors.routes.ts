import { Routes } from '@angular/router';

export const vendorsRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/vendor-list/vendor-list').then(m => m.VendorListComponent),
  },
];
