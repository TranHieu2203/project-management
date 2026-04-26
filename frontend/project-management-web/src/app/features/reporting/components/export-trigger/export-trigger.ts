import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  inject,
  OnDestroy,
} from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Subject, interval } from 'rxjs';
import { startWith, switchMap, takeUntil } from 'rxjs/operators';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { ReportingApiService } from '../../services/reporting-api.service';
import { ExportJobDto } from '../../models/cost-report.model';

@Component({
  selector: 'app-export-trigger',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './export-trigger.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExportTriggerComponent implements OnDestroy {
  private readonly api = inject(ReportingApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly fb = inject(FormBuilder);
  private readonly destroy$ = new Subject<void>();
  private readonly stopPolling$ = new Subject<void>();

  readonly formats = [
    { value: 'csv',  label: 'CSV' },
    { value: 'xlsx', label: 'Excel (XLSX)' },
    { value: 'pdf',  label: 'PDF' },
  ];

  readonly dimensions = [
    { value: 'vendor', label: 'Theo Vendor' },
    { value: 'project', label: 'Theo Project' },
    { value: 'resource', label: 'Theo Nhân sự' },
    { value: 'month', label: 'Theo Tháng' },
  ];

  readonly form = this.fb.nonNullable.group({
    format: ['csv'],
    groupBy: ['vendor'],
    month: [''],
  });

  loading = false;
  jobStatus: ExportJobDto | null = null;

  trigger(): void {
    if (this.loading) return;
    const { format, groupBy, month } = this.form.getRawValue();
    this.loading = true;
    this.jobStatus = null;
    this.stopPolling$.next();

    this.api.triggerExport(format, groupBy, month || undefined)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ jobId }) => this.startPolling(jobId),
        error: () => { this.loading = false; this.cdr.markForCheck(); },
      });
  }

  private startPolling(jobId: string): void {
    interval(2000).pipe(
      startWith(0),
      switchMap(() => this.api.getExportJobStatus(jobId)),
      takeUntil(this.stopPolling$),
      takeUntil(this.destroy$),
    ).subscribe(status => {
      this.jobStatus = status;
      if (status.status === 'Ready' || status.status === 'Failed') {
        this.loading = false;
        this.stopPolling$.next();
      }
      this.cdr.markForCheck();
    });
  }

  download(): void {
    if (!this.jobStatus?.jobId) return;
    this.api.downloadExport(this.jobStatus.jobId).subscribe(blob => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = this.jobStatus?.fileName ?? 'export';
      a.click();
      URL.revokeObjectURL(url);
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.stopPolling$.complete();
  }
}
