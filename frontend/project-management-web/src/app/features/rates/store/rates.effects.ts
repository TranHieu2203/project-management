import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, switchMap } from 'rxjs/operators';
import { of } from 'rxjs';
import { RatesActions } from './rates.actions';
import { RatesApiService } from '../services/rates-api.service';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable()
export class RatesEffects {
  private readonly actions$ = inject(Actions);
  private readonly ratesApi = inject(RatesApiService);

  loadRates$ = createEffect(() =>
    this.actions$.pipe(
      ofType(RatesActions.loadRates),
      switchMap(({ vendorId, year, month }) =>
        this.ratesApi.getRates(vendorId, year, month).pipe(
          map(rates => RatesActions.loadRatesSuccess({ rates })),
          catchError((err: HttpErrorResponse) =>
            of(RatesActions.loadRatesFailure({ error: err.error?.detail ?? 'Không thể tải danh sách rate.' }))
          )
        )
      )
    )
  );

  createRate$ = createEffect(() =>
    this.actions$.pipe(
      ofType(RatesActions.createRate),
      switchMap(({ vendorId, role, level, year, month, monthlyAmount }) =>
        this.ratesApi.createRate({ vendorId, role, level, year, month, monthlyAmount }).pipe(
          map(rate => RatesActions.createRateSuccess({ rate })),
          catchError((err: HttpErrorResponse) =>
            of(RatesActions.createRateFailure({ error: err.error?.detail ?? 'Không thể tạo rate.' }))
          )
        )
      )
    )
  );

  deleteRate$ = createEffect(() =>
    this.actions$.pipe(
      ofType(RatesActions.deleteRate),
      switchMap(({ rateId }) =>
        this.ratesApi.deleteRate(rateId).pipe(
          map(() => RatesActions.deleteRateSuccess({ rateId })),
          catchError((err: HttpErrorResponse) =>
            of(RatesActions.deleteRateFailure({ error: err.error?.detail ?? 'Không thể xóa rate.' }))
          )
        )
      )
    )
  );
}
