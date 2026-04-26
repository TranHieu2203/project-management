import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, switchMap } from 'rxjs/operators';
import { of } from 'rxjs';
import { ResourcesActions } from './resources.actions';
import { ResourcesApiService } from '../services/resources-api.service';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable()
export class ResourcesEffects {
  private readonly actions$ = inject(Actions);
  private readonly resourcesApi = inject(ResourcesApiService);

  loadResources$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ResourcesActions.loadResources),
      switchMap(({ resourceType, vendorId, activeOnly }) =>
        this.resourcesApi.getResources(resourceType, vendorId, activeOnly).pipe(
          map(resources => ResourcesActions.loadResourcesSuccess({ resources })),
          catchError((err: HttpErrorResponse) =>
            of(ResourcesActions.loadResourcesFailure({ error: err.error?.detail ?? 'Không thể tải danh sách resource.' }))
          )
        )
      )
    )
  );

  createResource$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ResourcesActions.createResource),
      switchMap(({ code, name, email, resourceType, vendorId }) =>
        this.resourcesApi.createResource(code, name, email, resourceType, vendorId).pipe(
          map(resource => ResourcesActions.createResourceSuccess({ resource })),
          catchError((err: HttpErrorResponse) =>
            of(ResourcesActions.createResourceFailure({ error: err.error?.detail ?? 'Không thể tạo resource.' }))
          )
        )
      )
    )
  );

  updateResource$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ResourcesActions.updateResource),
      switchMap(({ resourceId, name, email, version }) =>
        this.resourcesApi.updateResource(resourceId, name, email, version).pipe(
          map(resource => ResourcesActions.updateResourceSuccess({ resource })),
          catchError((err: HttpErrorResponse) => {
            if (err.status === 409) {
              const serverState = err.error?.extensions?.current;
              const eTag = err.error?.extensions?.eTag ?? `"${version}"`;
              if (serverState) {
                return of(ResourcesActions.updateResourceConflict({
                  serverState,
                  eTag,
                  pendingName: name,
                  pendingEmail: email,
                }));
              }
            }
            return of(ResourcesActions.updateResourceFailure({ error: err.error?.detail ?? 'Không thể cập nhật resource.' }));
          })
        )
      )
    )
  );

  inactivateResource$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ResourcesActions.inactivateResource),
      switchMap(({ resourceId, version }) =>
        this.resourcesApi.inactivateResource(resourceId, version).pipe(
          switchMap(() => this.resourcesApi.getResourceById(resourceId)),
          map(resource => ResourcesActions.inactivateResourceSuccess({ resource })),
          catchError((err: HttpErrorResponse) =>
            of(ResourcesActions.inactivateResourceFailure({ error: err.error?.detail ?? 'Không thể inactivate resource.' }))
          )
        )
      )
    )
  );
}
