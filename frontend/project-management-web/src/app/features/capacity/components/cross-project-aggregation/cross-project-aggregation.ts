import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AsyncPipe, DecimalPipe, NgClass, NgIf } from '@angular/common';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { CapacityActions } from '../../store/capacity.actions';
import { selectCrossProject, selectCrossProjectLoading } from '../../store/capacity.reducer';

@Component({
  selector: 'app-cross-project-aggregation',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    AsyncPipe,
    DecimalPipe,
    NgIf,
    NgClass,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTableModule,
  ],
  templateUrl: './cross-project-aggregation.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CrossProjectAggregationComponent {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);

  readonly result$ = this.store.select(selectCrossProject);
  readonly loading$ = this.store.select(selectCrossProjectLoading);

  readonly form = this.fb.nonNullable.group({
    dateFrom: ['', Validators.required],
    dateTo: ['', Validators.required],
  });

  readonly columns = ['resourceId', 'totalHours', 'overloadedDays', 'overloadedWeeks', 'status'];

  load(): void {
    if (this.form.invalid) return;
    const { dateFrom, dateTo } = this.form.getRawValue();
    this.store.dispatch(CapacityActions.loadCrossProject({ dateFrom, dateTo }));
  }
}
