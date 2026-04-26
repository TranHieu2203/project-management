import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, of, switchMap } from 'rxjs';
import { ProjectsApiService } from '../services/projects-api.service';
import { ProjectsActions } from './projects.actions';

@Injectable()
export class ProjectsEffects {
  private readonly actions$ = inject(Actions);
  private readonly projectsApiService = inject(ProjectsApiService);

  loadProjects$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ProjectsActions.loadProjects),
      switchMap(() =>
        this.projectsApiService.getProjects().pipe(
          map(projects => ProjectsActions.loadProjectsSuccess({ projects })),
          catchError(err =>
            of(
              ProjectsActions.loadProjectsFailure({
                error:
                  err.error?.detail ??
                  err.error?.title ??
                  'Không thể tải danh sách dự án. Vui lòng thử lại.',
              })
            )
          )
        )
      )
    )
  );

  createProject$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ProjectsActions.createProject),
      switchMap(action =>
        this.projectsApiService.createProject(action.code, action.name, action.description).pipe(
          map(project => ProjectsActions.createProjectSuccess({ project })),
          catchError(err =>
            of(
              ProjectsActions.createProjectFailure({
                error:
                  err.error?.detail ??
                  err.error?.title ??
                  'Không thể tạo dự án. Vui lòng thử lại.',
              })
            )
          )
        )
      )
    )
  );

  updateProject$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ProjectsActions.updateProject),
      switchMap(action =>
        this.projectsApiService
          .updateProject(action.projectId, action.name, action.description, action.version)
          .pipe(
            map(project => ProjectsActions.updateProjectSuccess({ project })),
            catchError(err => {
              if (err.status === 409) {
                // current và eTag ở ROOT level của body (không phải extensions.current)
                return of(
                  ProjectsActions.updateProjectConflict({
                    serverState: err.error.current,
                    eTag: err.error.eTag,
                    pendingName: action.name,
                    pendingDescription: action.description,
                  })
                );
              }
              return of(
                ProjectsActions.updateProjectFailure({
                  error:
                    err.error?.detail ??
                    err.error?.title ??
                    'Không thể cập nhật dự án.',
                })
              );
            })
          )
      )
    )
  );

  deleteProject$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ProjectsActions.deleteProject),
      switchMap(action =>
        this.projectsApiService.deleteProject(action.projectId, action.version).pipe(
          map(() => ProjectsActions.deleteProjectSuccess({ projectId: action.projectId })),
          catchError(err =>
            of(
              ProjectsActions.deleteProjectFailure({
                error:
                  err.error?.detail ??
                  err.error?.title ??
                  'Không thể xóa dự án.',
              })
            )
          )
        )
      )
    )
  );
}
