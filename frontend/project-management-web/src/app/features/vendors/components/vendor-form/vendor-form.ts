import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Vendor } from '../../models/vendor.model';

export interface VendorFormData {
  vendor: Vendor | null;
}

@Component({
  selector: 'app-vendor-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  templateUrl: './vendor-form.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VendorFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<VendorFormComponent>);
  readonly data = inject<VendorFormData>(MAT_DIALOG_DATA);

  readonly isEdit = !!this.data.vendor;

  form = this.fb.group({
    code: [{ value: '', disabled: this.isEdit }, [Validators.required, Validators.maxLength(50)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
  });

  ngOnInit(): void {
    if (this.data.vendor) {
      this.form.patchValue({
        code: this.data.vendor.code,
        name: this.data.vendor.name,
        description: this.data.vendor.description ?? '',
      });
    }
  }

  submit(): void {
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    this.dialogRef.close({
      code: value.code ?? '',
      name: value.name ?? '',
      description: value.description || undefined,
    });
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}
