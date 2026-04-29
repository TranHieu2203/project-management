import { Routes } from '@angular/router';

export const projectsRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/project-list/project-list').then(
        m => m.ProjectListComponent
      ),
  },
  {
    path: ':projectId',
    loadComponent: () =>
      import('./components/project-detail/project-detail').then(
        m => m.ProjectDetailComponent
      ),
  },
];
