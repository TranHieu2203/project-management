import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Store } from '@ngrx/store';
import { AsyncPipe } from '@angular/common';
import { take } from 'rxjs/operators';
import { selectRoles, selectLevels, selectLookupsLoaded } from '../../../lookups/store/lookups.selectors';
import { selectAllVendors } from '../../../vendors/store/vendors.selectors';
import { LookupsActions } from '../../../lookups/store/lookups.actions';
import { VendorsActions } from '../../../vendors/store/vendors.actions';

export interface RateFormData {
  vendorId?: string;
}

@Component({
  selector: 'app-rate-form',
  standalone: true,
  imports: [
    AsyncPipe,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
  ],
  templateUrl: './rate-form.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RateFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<RateFormComponent>);
  private readonly store = inject(Store);
  readonly data = inject<RateFormData>(MAT_DIALOG_DATA);

  readonly vendors$ = this.store.select(selectAllVendors);
  readonly roles$ = this.store.select(selectRoles);
  readonly levels$ = this.store.select(selectLevels);

  readonly currentYear = new Date().getFullYear();
  readonly years = Array.from({ length: 5 }, (_, i) => this.currentYear - 1 + i);
  readonly months = Array.from({ length: 12 }, (_, i) => ({ value: i + 1, label: `Tháng ${i + 1}` }));

  form = this.fb.group({
    vendorId: [this.data.vendorId ?? '', [Validators.required]],
    role: ['', [Validators.required]],
    level: ['', [Validators.required]],
    year: [this.currentYear, [Validators.required]],
    month: [new Date().getMonth() + 1, [Validators.required]],
    monthlyAmount: [null as number | null, [Validators.required, Validators.min(1)]],
  });

  ngOnInit(): void {
    this.store.dispatch(VendorsActions.loadVendors({ activeOnly: true }));
    this.store.select(selectLookupsLoaded).pipe(take(1)).subscribe(loaded => {
      if (!loaded) this.store.dispatch(LookupsActions.loadCatalog());
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    this.dialogRef.close({
      vendorId: value.vendorId!,
      role: value.role!,
      level: value.level!,
      year: value.year!,
      month: value.month!,
      monthlyAmount: value.monthlyAmount!,
    });
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}
