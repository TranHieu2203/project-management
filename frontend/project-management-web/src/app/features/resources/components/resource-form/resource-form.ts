import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Store } from '@ngrx/store';
import { AsyncPipe } from '@angular/common';
import { Resource } from '../../models/resource.model';
import { selectAllVendors } from '../../../vendors/store/vendors.selectors';
import { VendorsActions } from '../../../vendors/store/vendors.actions';

export interface ResourceFormData {
  resource: Resource | null;
}

@Component({
  selector: 'app-resource-form',
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
  templateUrl: './resource-form.html',
  styleUrl: './resource-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourceFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<ResourceFormComponent>);
  private readonly store = inject(Store);
  readonly data = inject<ResourceFormData>(MAT_DIALOG_DATA);

  readonly isEdit = !!this.data.resource;
  readonly vendors$ = this.store.select(selectAllVendors);

  form = this.fb.group({
    code: [{ value: '', disabled: this.isEdit }, [Validators.required, Validators.maxLength(50)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.email, Validators.maxLength(256)]],
    type: [{ value: 'Inhouse', disabled: this.isEdit }, [Validators.required]],
    vendorId: [{ value: null as string | null, disabled: this.isEdit }],
  });

  ngOnInit(): void {
    this.store.dispatch(VendorsActions.loadVendors({ activeOnly: true }));

    if (this.data.resource) {
      this.form.patchValue({
        code: this.data.resource.code,
        name: this.data.resource.name,
        email: this.data.resource.email ?? '',
        type: this.data.resource.type,
        vendorId: this.data.resource.vendorId ?? null,
      });
    }
  }

  get selectedType(): string {
    return this.form.getRawValue().type ?? 'Inhouse';
  }

  submit(): void {
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    this.dialogRef.close({
      code: value.code ?? '',
      name: value.name ?? '',
      email: value.email || undefined,
      type: value.type ?? 'Inhouse',
      vendorId: value.vendorId || undefined,
    });
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}
