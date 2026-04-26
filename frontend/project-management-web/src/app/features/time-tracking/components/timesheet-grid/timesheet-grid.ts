import {
  ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit, signal
} from '@angular/core';
import { FormArray, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TimeTrackingActions } from '../../store/time-tracking.actions';
import { BulkTimesheetRow, BulkValidationError } from '../../models/bulk-timesheet.model';
import { ProjectsApiService } from '../../../projects/services/projects-api.service';
import { ResourcesApiService } from '../../../resources/services/resources-api.service';
import { Project } from '../../../projects/models/project.model';
import { Resource } from '../../../resources/models/resource.model';

interface ResourceRow {
  resourceId: string;
  label: string;
  projectId: string;
  role: string;
  level: string;
}

const ROLES = ['Developer', 'Designer', 'QA', 'PM', 'DevOps', 'BA', 'Other'];
const LEVELS = ['Junior', 'Mid', 'Senior', 'Lead'];

@Component({
  selector: 'app-timesheet-grid',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatFormFieldModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './timesheet-grid.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TimesheetGridComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly projectsApi = inject(ProjectsApiService);
  private readonly resourcesApi = inject(ResourcesApiService);

  readonly roles = ROLES;
  readonly levels = LEVELS;

  readonly projects = signal<Project[]>([]);
  readonly resources = signal<ResourceRow[]>([]);
  readonly loadingResources = signal(false);

  selectedProjectId = signal<string | null>(null);

  gridState: 'idle' | 'loading' | 'error' = 'idle';
  errorMessage = '';
  validationErrors: BulkValidationError[] = [];

  weekStart: Date = this.getMonday(new Date());
  weekDays: Date[] = [];

  readonly entryType = 'PmAdjusted';

  form!: FormGroup;

  ngOnInit(): void {
    this.buildWeek();
    this.buildForm();
    this.projectsApi.getProjects().subscribe({
      next: (list) => {
        this.projects.set(list);
        this.cdr.markForCheck();
      },
      error: () => this.cdr.markForCheck(),
    });
  }

  onProjectChange(projectId: string): void {
    this.selectedProjectId.set(projectId);
    this.loadingResources.set(true);
    this.resources.set([]);
    this.cdr.markForCheck();

    this.resourcesApi.getResources(undefined, undefined, true).subscribe({
      next: (list: Resource[]) => {
        const rows: ResourceRow[] = list.map(r => ({
          resourceId: r.id,
          label: r.name,
          projectId,
          role: 'Developer',
          level: 'Junior',
        }));
        this.resources.set(rows);
        this.loadingResources.set(false);
        this.buildForm();
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadingResources.set(false);
        this.cdr.markForCheck();
      },
    });
  }

  get rows(): FormArray {
    return this.form.get('rows') as FormArray;
  }

  buildWeek(): void {
    this.weekDays = Array.from({ length: 7 }, (_, i) => {
      const d = new Date(this.weekStart);
      d.setDate(d.getDate() + i);
      return d;
    });
  }

  buildForm(): void {
    const rowGroups = this.resources().map(r =>
      this.fb.group({
        role: [r.role, Validators.required],
        level: [r.level, Validators.required],
        cells: this.fb.array(
          this.weekDays.map(() => this.fb.control<number | null>(null, [Validators.min(0), Validators.max(24)]))
        ),
        notes: this.fb.array(this.weekDays.map(() => this.fb.control(''))),
      })
    );
    this.form = this.fb.group({ rows: this.fb.array(rowGroups) });
  }

  getCells(rowIndex: number): FormArray {
    return (this.rows.at(rowIndex) as FormGroup).get('cells') as FormArray;
  }

  getNotes(rowIndex: number): FormArray {
    return (this.rows.at(rowIndex) as FormGroup).get('notes') as FormArray;
  }

  getRoleCtrl(rowIndex: number): FormControl {
    return (this.rows.at(rowIndex) as FormGroup).get('role') as FormControl;
  }

  getLevelCtrl(rowIndex: number): FormControl {
    return (this.rows.at(rowIndex) as FormGroup).get('level') as FormControl;
  }

  prevWeek(): void {
    this.weekStart.setDate(this.weekStart.getDate() - 7);
    this.weekStart = new Date(this.weekStart);
    this.buildWeek();
    this.buildForm();
    this.validationErrors = [];
  }

  nextWeek(): void {
    this.weekStart.setDate(this.weekStart.getDate() + 7);
    this.weekStart = new Date(this.weekStart);
    this.buildWeek();
    this.buildForm();
    this.validationErrors = [];
  }

  formatDay(d: Date): string {
    return d.toLocaleDateString('vi-VN', { weekday: 'short', month: '2-digit', day: '2-digit' });
  }

  formatDate(d: Date): string {
    return d.toISOString().split('T')[0];
  }

  hasHardError(rowIndex: number, colIndex: number): boolean {
    const idx = rowIndex * this.weekDays.length + colIndex;
    return this.validationErrors.some(e => e.rowIndex === idx && e.errorType === 'hard');
  }

  hasWarning(rowIndex: number, colIndex: number): boolean {
    const idx = rowIndex * this.weekDays.length + colIndex;
    return this.validationErrors.some(e => e.rowIndex === idx && e.errorType === 'warning');
  }

  onKeydown(event: KeyboardEvent, rowIndex: number, colIndex: number): void {
    if (event.key === 'Tab' || event.key === 'Enter') {
      event.preventDefault();
      const nextCol = colIndex + 1;
      const nextRow = rowIndex + (nextCol >= this.weekDays.length ? 1 : 0);
      const nextColWrapped = nextCol % this.weekDays.length;
      if (nextRow < this.resources().length) {
        (document.getElementById(`cell-${nextRow}-${nextColWrapped}`) as HTMLInputElement)?.focus();
      }
    }
    if (event.key === 'Escape') {
      this.getCells(rowIndex).at(colIndex).setValue(null);
    }
  }

  submit(): void {
    if (!this.selectedProjectId()) return;
    const rows: BulkTimesheetRow[] = [];

    for (let r = 0; r < this.resources().length; r++) {
      const resource = this.resources()[r];
      const rowGroup = this.rows.at(r) as FormGroup;
      const role = rowGroup.get('role')!.value as string;
      const level = rowGroup.get('level')!.value as string;
      const cells = this.getCells(r);
      const notes = this.getNotes(r);

      for (let c = 0; c < this.weekDays.length; c++) {
        const hours = cells.at(c).value as number | null;
        if (!hours || hours <= 0) continue;
        rows.push({
          resourceId: resource.resourceId,
          projectId: this.selectedProjectId()!,
          date: this.formatDate(this.weekDays[c]),
          hours,
          entryType: this.entryType,
          role,
          level,
          note: (notes.at(c).value as string) || undefined,
        });
      }
    }

    if (rows.length === 0) return;

    this.gridState = 'loading';
    this.validationErrors = [];
    this.cdr.markForCheck();
    this.store.dispatch(TimeTrackingActions.submitBulk({ rows }));
  }

  retry(): void {
    this.gridState = 'idle';
    this.errorMessage = '';
    this.cdr.markForCheck();
  }

  private getMonday(d: Date): Date {
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    return new Date(d.setDate(diff));
  }
}
