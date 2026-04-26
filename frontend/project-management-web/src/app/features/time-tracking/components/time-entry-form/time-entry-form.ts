import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AsyncPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Store } from '@ngrx/store';
import { take } from 'rxjs/operators';
import { selectRoles, selectLevels, selectLookupsLoaded } from '../../../lookups/store/lookups.selectors';
import { LookupsActions } from '../../../lookups/store/lookups.actions';

export interface TimeEntryFormData {
  projectId?: string;
  resourceId?: string;
  supersededEntryId?: string;
}

@Component({
  selector: 'app-time-entry-form',
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
  templateUrl: './time-entry-form.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TimeEntryFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<TimeEntryFormComponent>);
  private readonly store = inject(Store);
  readonly data = inject<TimeEntryFormData>(MAT_DIALOG_DATA);

  readonly roles$ = this.store.select(selectRoles);
  readonly levels$ = this.store.select(selectLevels);

  readonly entryTypes = [
    { value: 'Estimated', label: 'Estimated (Ước tính)' },
    { value: 'PmAdjusted', label: 'PM Adjusted (Điều chỉnh)' },
  ];

  readonly today = new Date().toISOString().split('T')[0];

  form = this.fb.group({
    resourceId: [this.data.resourceId ?? '', [Validators.required]],
    projectId: [this.data.projectId ?? '', [Validators.required]],
    taskId: [''],
    date: [this.today, [Validators.required]],
    hours: [null as number | null, [Validators.required, Validators.min(0.25), Validators.max(24)]],
    entryType: ['PmAdjusted', [Validators.required]],
    role: ['', [Validators.required]],
    level: ['', [Validators.required]],
    note: [''],
  });

  ngOnInit(): void {
    this.store.select(selectLookupsLoaded).pipe(take(1)).subscribe(loaded => {
      if (!loaded) this.store.dispatch(LookupsActions.loadCatalog());
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.value;
    this.dialogRef.close({
      resourceId: v.resourceId,
      projectId: v.projectId,
      taskId: v.taskId || undefined,
      date: v.date,
      hours: v.hours,
      entryType: v.entryType,
      role: v.role,
      level: v.level,
      note: v.note || undefined,
      supersededEntryId: this.data.supersededEntryId,
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
