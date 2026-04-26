import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, switchMap } from 'rxjs/operators';
import { of } from 'rxjs';
import { VendorsActions } from './vendors.actions';
import { VendorsApiService } from '../services/vendors-api.service';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable()
export class VendorsEffects {
  private readonly actions$ = inject(Actions);
  private readonly vendorsApi = inject(VendorsApiService);

  loadVendors$ = createEffect(() =>
    this.actions$.pipe(
      ofType(VendorsActions.loadVendors),
      switchMap(({ activeOnly }) =>
        this.vendorsApi.getVendors(activeOnly).pipe(
          map(vendors => VendorsActions.loadVendorsSuccess({ vendors })),
          catchError((err: HttpErrorResponse) =>
            of(VendorsActions.loadVendorsFailure({ error: err.error?.detail ?? 'Không thể tải danh sách vendor.' }))
          )
        )
      )
    )
  );

  createVendor$ = createEffect(() =>
    this.actions$.pipe(
      ofType(VendorsActions.createVendor),
      switchMap(({ code, name, description }) =>
        this.vendorsApi.createVendor(code, name, description).pipe(
          map(vendor => VendorsActions.createVendorSuccess({ vendor })),
          catchError((err: HttpErrorResponse) =>
            of(VendorsActions.createVendorFailure({ error: err.error?.detail ?? 'Không thể tạo vendor.' }))
          )
        )
      )
    )
  );

  updateVendor$ = createEffect(() =>
    this.actions$.pipe(
      ofType(VendorsActions.updateVendor),
      switchMap(({ vendorId, name, description, version }) =>
        this.vendorsApi.updateVendor(vendorId, name, description, version).pipe(
          map(vendor => VendorsActions.updateVendorSuccess({ vendor })),
          catchError((err: HttpErrorResponse) => {
            if (err.status === 409) {
              const serverState = err.error?.extensions?.current;
              const eTag = err.error?.extensions?.eTag ?? `"${version}"`;
              if (serverState) {
                return of(VendorsActions.updateVendorConflict({
                  serverState,
                  eTag,
                  pendingName: name,
                  pendingDescription: description,
                }));
              }
            }
            return of(VendorsActions.updateVendorFailure({ error: err.error?.detail ?? 'Không thể cập nhật vendor.' }));
          })
        )
      )
    )
  );

  inactivateVendor$ = createEffect(() =>
    this.actions$.pipe(
      ofType(VendorsActions.inactivateVendor),
      switchMap(({ vendorId, version }) =>
        this.vendorsApi.inactivateVendor(vendorId, version).pipe(
          switchMap(() => this.vendorsApi.getVendorById(vendorId)),
          map(vendor => VendorsActions.inactivateVendorSuccess({ vendor })),
          catchError((err: HttpErrorResponse) =>
            of(VendorsActions.inactivateVendorFailure({ error: err.error?.detail ?? 'Không thể inactivate vendor.' }))
          )
        )
      )
    )
  );
}
