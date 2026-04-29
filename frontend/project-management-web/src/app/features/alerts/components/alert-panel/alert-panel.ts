import { ChangeDetectionStrategy, Component, EventEmitter, Output, inject } from '@angular/core';
import { AsyncPipe, DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AlertActions } from '../../store/alert.actions';
import { selectAlerts, selectLoading } from '../../store/alert.reducer';
import { AlertDto } from '../../models/alert.model';

@Component({
  selector: 'app-alert-panel',
  standalone: true,
  imports: [
    AsyncPipe, DatePipe, NgFor, NgIf, NgClass,
    MatButtonModule, MatIconModule, MatDividerModule, MatProgressSpinnerModule,
  ],
  templateUrl: './alert-panel.html',
  styleUrl: './alert-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AlertPanelComponent {
  private readonly store = inject(Store);

  readonly alerts$ = this.store.select(selectAlerts);
  readonly loading$ = this.store.select(selectLoading);

  @Output() closed = new EventEmitter<void>();

  trackAlert(_: number, alert: AlertDto): string { return alert.id; }

  onAlertClick(alert: AlertDto): void {
    this.store.dispatch(AlertActions.markAlertRead({
      id: alert.id,
      projectId: alert.projectId,
      entityType: alert.entityType,
      entityId: alert.entityId,
    }));
    this.store.dispatch(AlertActions.closePanel());
    this.closed.emit();
  }

  typeLabel(type: string): string {
    const labels: Record<string, string> = {
      deadline: 'Deadline',
      overload: 'Quá tải',
      budget: 'Budget',
    };
    return labels[type] ?? type;
  }
}
