import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, switchMap } from 'rxjs/operators';
import { forkJoin, of } from 'rxjs';
import { LookupsActions } from './lookups.actions';
import { LookupsApiService } from '../services/lookups-api.service';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable()
export class LookupsEffects {
  private readonly actions$ = inject(Actions);
  private readonly lookupsApi = inject(LookupsApiService);

  loadCatalog$ = createEffect(() =>
    this.actions$.pipe(
      ofType(LookupsActions.loadCatalog),
      switchMap(() =>
        forkJoin({
          roles: this.lookupsApi.getRoles(),
          levels: this.lookupsApi.getLevels(),
        }).pipe(
          map(({ roles, levels }) => LookupsActions.loadCatalogSuccess({ roles, levels })),
          catchError((err: HttpErrorResponse) =>
            of(LookupsActions.loadCatalogFailure({ error: err.error?.detail ?? 'Không thể tải catalog.' }))
          )
        )
      )
    )
  );
}
