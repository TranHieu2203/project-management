import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { DatePipe, DecimalPipe, NgClass, NgIf } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TimeTrackingApiService } from '../../services/time-tracking-api.service';
import { PeriodReconcileDto } from '../../models/period-lock.model';

type PageState = 'idle' | 'loading' | 'loaded' | 'locking' | 'unlocking' | 'error';

@Component({
  selector: 'app-period-lock',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    NgIf,
    NgClass,
    DatePipe,
    DecimalPipe,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './period-lock.html',
  changeDetection: ChangeDetectionStrategy.Default,
})
export class PeriodLockComponent {
  private readonly api = inject(TimeTrackingApiService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly form = this.fb.nonNullable.group({
    vendorId: ['', Validators.required],
    year: [new Date().getFullYear(), [Validators.required, Validators.min(2020), Validators.max(2099)]],
    month: [new Date().getMonth() + 1, [Validators.required, Validators.min(1), Validators.max(12)]],
  });

  state: PageState = 'idle';
  reconcile: PeriodReconcileDto | null = null;
  errorMessage = '';

  load(): void {
    if (this.form.invalid) return;
    const { vendorId, year, month } = this.form.getRawValue();
    this.state = 'loading';
    this.errorMessage = '';
    this.api.getPeriodReconcile(vendorId, year, month).subscribe({
      next: data => {
        this.reconcile = data;
        this.state = 'loaded';
        this.cdr.markForCheck();
      },
      error: err => {
        this.state = 'error';
        this.errorMessage = err?.error?.detail ?? 'Không thể tải dữ liệu.';
        this.cdr.markForCheck();
      },
    });
  }

  lockPeriod(): void {
    if (!this.reconcile) return;
    if (!confirm(`Lock kỳ ${this.reconcile.year}/${String(this.reconcile.month).padStart(2, '0')}? Sau khi lock, không thể void/import thêm VendorConfirmed.`)) return;
    this.state = 'locking';
    const { vendorId, year, month } = this.form.getRawValue();
    this.api.lockPeriod(vendorId, year, month).subscribe({
      next: () => this.load(),
      error: err => {
        this.state = 'error';
        this.errorMessage = err?.error?.detail ?? 'Lock thất bại.';
        this.cdr.markForCheck();
      },
    });
  }

  unlockPeriod(): void {
    if (!this.reconcile) return;
    if (!confirm(`Unlock kỳ ${this.reconcile.year}/${String(this.reconcile.month).padStart(2, '0')}? Thao tác này chỉ dành cho admin.`)) return;
    this.state = 'unlocking';
    const { vendorId, year, month } = this.form.getRawValue();
    this.api.unlockPeriod(vendorId, year, month).subscribe({
      next: () => this.load(),
      error: err => {
        this.state = 'error';
        this.errorMessage = err?.error?.detail ?? 'Unlock thất bại.';
        this.cdr.markForCheck();
      },
    });
  }
}
