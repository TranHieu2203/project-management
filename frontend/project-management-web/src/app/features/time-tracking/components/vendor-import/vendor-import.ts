import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { NgClass, NgIf } from '@angular/common';
import { Subject, interval } from 'rxjs';
import { switchMap, takeUntil } from 'rxjs/operators';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { TimeTrackingApiService } from '../../services/time-tracking-api.service';
import { ColumnMapping, ImportJobDto, ImportJobErrorDto } from '../../models/import-job.model';

type PageState = 'idle' | 'uploading' | 'ready-to-apply' | 'applying' | 'done' | 'error';

@Component({
  selector: 'app-vendor-import',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    NgIf,
    NgClass,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTableModule,
  ],
  templateUrl: './vendor-import.html',
  changeDetection: ChangeDetectionStrategy.Default,
})
export class VendorImportComponent implements OnDestroy {
  private readonly api = inject(TimeTrackingApiService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroy$ = new Subject<void>();
  private readonly stopPolling$ = new Subject<void>();

  readonly form = this.fb.nonNullable.group({
    vendorId: ['', Validators.required],
    resourceIdColumn: ['resource_id', Validators.required],
    projectIdColumn: ['project_id', Validators.required],
    dateColumn: ['date', Validators.required],
    hoursColumn: ['hours', Validators.required],
    roleColumn: ['role', Validators.required],
    levelColumn: ['level', Validators.required],
    noteColumn: [''],
    taskIdColumn: [''],
  });

  selectedFile: File | null = null;
  state: PageState = 'idle';
  job: ImportJobDto | null = null;
  errors: ImportJobErrorDto[] = [];
  errorMessage = '';

  readonly errorColumns = ['rowIndex', 'columnName', 'errorType', 'message'];

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  submit(): void {
    if (this.form.invalid || !this.selectedFile) return;
    const val = this.form.getRawValue();
    const mapping = this.buildMapping(val);
    this.state = 'uploading';
    this.errorMessage = '';
    this.api.startImportJob(this.selectedFile, val.vendorId, mapping).subscribe({
      next: job => {
        this.job = job;
        if (this.isProcessing(job.status)) {
          this.startPolling(job.id);
        } else {
          this.state = this.toPageState(job.status);
          this.loadErrors(job.id);
        }
        this.cdr.markForCheck();
      },
      error: err => {
        this.state = 'error';
        this.errorMessage = err?.error?.detail ?? 'Upload thất bại.';
        this.cdr.markForCheck();
      },
    });
  }

  applyJob(): void {
    if (!this.job) return;
    const hasWarnings = this.job.status === 'ValidatedWithWarnings';
    if (hasWarnings && !confirm(`Job có ${this.job.errorCount} cảnh báo (warning). Vẫn muốn apply?`)) return;
    const val = this.form.getRawValue();
    const mapping = this.buildMapping(val);
    this.state = 'applying';
    this.api.applyImportJob(this.job.id, mapping).subscribe({
      next: job => {
        this.job = job;
        this.state = 'done';
        this.cdr.markForCheck();
      },
      error: err => {
        this.state = 'error';
        this.errorMessage = err?.error?.detail ?? 'Apply thất bại.';
        this.cdr.markForCheck();
      },
    });
  }

  downloadErrors(): void {
    if (!this.job) return;
    window.open(`/api/v1/import-jobs/${this.job.id}/errors/download`);
  }

  reset(): void {
    this.stopPolling$.next();
    this.state = 'idle';
    this.job = null;
    this.errors = [];
    this.selectedFile = null;
    this.errorMessage = '';
    this.form.reset({ resourceIdColumn: 'resource_id', projectIdColumn: 'project_id', dateColumn: 'date', hoursColumn: 'hours', roleColumn: 'role', levelColumn: 'level', noteColumn: '', taskIdColumn: '' });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.stopPolling$.next();
    this.stopPolling$.complete();
  }

  private buildMapping(val: ReturnType<typeof this.form.getRawValue>): ColumnMapping {
    return {
      resourceIdColumn: val.resourceIdColumn,
      projectIdColumn: val.projectIdColumn,
      dateColumn: val.dateColumn,
      hoursColumn: val.hoursColumn,
      roleColumn: val.roleColumn,
      levelColumn: val.levelColumn,
      noteColumn: val.noteColumn || undefined,
      taskIdColumn: val.taskIdColumn || undefined,
    };
  }

  private loadErrors(jobId: string): void {
    this.api.getImportJobErrors(jobId).subscribe({
      next: errs => {
        this.errors = errs;
        this.cdr.markForCheck();
      },
    });
  }

  private startPolling(jobId: string): void {
    this.stopPolling$.next();
    interval(3000).pipe(
      takeUntil(this.stopPolling$),
      takeUntil(this.destroy$),
      switchMap(() => this.api.getImportJob(jobId)),
    ).subscribe(job => {
      this.job = job;
      if (!this.isProcessing(job.status)) {
        this.stopPolling$.next();
        this.loadErrors(job.id);
        this.state = this.toPageState(job.status);
      }
      this.cdr.markForCheck();
    });
  }

  private isProcessing(status: string): boolean {
    return status === 'Pending' || status === 'Validating' || status === 'Applying';
  }

  private toPageState(status: string): PageState {
    if (status === 'ValidatedOk' || status === 'ValidatedWithWarnings') return 'ready-to-apply';
    if (status === 'ValidatedWithErrors') return 'error';
    if (status === 'Completed') return 'done';
    return 'error';
  }
}
