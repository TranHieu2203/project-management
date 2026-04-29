import {
  ChangeDetectionStrategy, ChangeDetectorRef, Component,
  EventEmitter, Input, Output, inject,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { ColumnDef, ColumnPickerService } from '../../services/column-picker.service';

@Component({
  selector: 'app-column-picker',
  standalone: true,
  imports: [FormsModule, MatButtonModule],
  templateUrl: './column-picker.component.html',
  styleUrl: './column-picker.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ColumnPickerComponent {
  @Input() componentId = '';
  @Input() columns: ColumnDef[] = [];
  @Output() changed = new EventEmitter<void>();

  private readonly service = inject(ColumnPickerService);
  private readonly cdr = inject(ChangeDetectorRef);

  isVisible(colId: string): boolean {
    return this.service.isVisible(this.componentId, colId);
  }

  toggle(colId: string): void {
    this.service.toggleColumn(this.componentId, colId, this.columns);
    this.changed.emit();
    this.cdr.markForCheck();
  }

  reset(): void {
    this.service.resetColumns(this.componentId, this.columns);
    this.changed.emit();
    this.cdr.markForCheck();
  }
}
