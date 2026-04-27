import {
  ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter,
  Input, OnChanges, Output, SimpleChanges, inject,
} from '@angular/core';
import { CommonModule, SlicePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { GanttTask } from '../../models/gantt.model';

export type GanttEditableField = 'name' | 'status' | 'plannedStart' | 'plannedEnd' | 'percentComplete';

export interface GanttInlineEditEvent {
  taskId: string;
  version: number;
  field: GanttEditableField;
  value: string | number | null;
}

@Component({
  selector: 'app-gantt-left-panel',
  standalone: true,
  imports: [CommonModule, SlicePipe, FormsModule, MatIconModule, MatButtonModule, MatTooltipModule],
  templateUrl: './gantt-left-panel.html',
  styleUrl: './gantt-left-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GanttLeftPanelComponent implements OnChanges {
  private readonly cdr = inject(ChangeDetectorRef);

  @Input() tasks: GanttTask[] = [];
  @Input() rowHeight = 36;
  @Input() scrollTop = 0;
  @Input() visibleMap: Map<string, boolean> | null = null;

  @Output() scrollChange = new EventEmitter<number>();
  @Output() taskCollapseToggle = new EventEmitter<GanttTask>();
  @Output() addChild = new EventEmitter<string>();
  @Output() editTask = new EventEmitter<string>();
  @Output() deleteTask = new EventEmitter<GanttTask>();
  @Output() inlineEdit = new EventEmitter<GanttInlineEditEvent>();

  visibleTasks: GanttTask[] = [];

  // ── Inline edit state ──────────────────────────────────────────────────────
  editingTaskId: string | null = null;
  editingField: GanttEditableField | null = null;
  editingValue = '';

  readonly statusOptions = ['NotStarted', 'InProgress', 'Completed', 'OnHold', 'Cancelled', 'Delayed'];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['tasks'] || changes['visibleMap']) {
      this.visibleTasks = this.getVisibleTasks();
    }
  }

  isFilterDim(taskId: string): boolean {
    return this.visibleMap?.get(taskId) === false;
  }

  isFilterHidden(taskId: string): boolean {
    return !!this.visibleMap && !this.visibleMap.has(taskId);
  }

  getVisibleTasks(): GanttTask[] {
    const taskMap = new Map(this.tasks.map(t => [t.id, t]));
    return this.tasks.filter(task =>
      !this.isHidden(task, taskMap) && !this.isFilterHidden(task.id));
  }

  private isHidden(task: GanttTask, taskMap: Map<string, GanttTask>): boolean {
    let parentId = task.parentId;
    while (parentId) {
      const parent = taskMap.get(parentId);
      if (!parent) break;
      if (parent.collapsed) return true;
      parentId = parent.parentId;
    }
    return false;
  }

  toggleCollapse(task: GanttTask, event: Event): void {
    event.stopPropagation();
    task.collapsed = !task.collapsed;
    this.visibleTasks = this.getVisibleTasks();
    this.taskCollapseToggle.emit(task);
  }

  hasChildren(task: GanttTask): boolean {
    return this.tasks.some(t => t.parentId === task.id);
  }

  onScroll(event: Event): void {
    const el = event.target as HTMLElement;
    this.scrollChange.emit(el.scrollTop);
  }

  getStatusBadgeClass(status: string): string {
    const map: Record<string, string> = {
      NotStarted: 'badge-grey',
      InProgress: 'badge-blue',
      Completed: 'badge-green',
      OnHold: 'badge-orange',
      Cancelled: 'badge-red',
      Delayed: 'badge-red',
    };
    return map[status] ?? 'badge-grey';
  }

  statusLabel(s: string): string {
    const map: Record<string, string> = {
      NotStarted: 'Chưa bắt đầu',
      InProgress: 'Đang làm',
      Completed: 'Hoàn thành',
      OnHold: 'Tạm dừng',
      Cancelled: 'Đã hủy',
      Delayed: 'Trễ hạn',
    };
    return map[s] ?? s;
  }

  formatDate(date: Date | null): string {
    if (!date) return '—';
    return date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' });
  }

  // ── Inline edit methods ────────────────────────────────────────────────────

  isEditing(taskId: string, field: GanttEditableField): boolean {
    return this.editingTaskId === taskId && this.editingField === field;
  }

  startEdit(task: GanttTask, field: GanttEditableField, event: Event): void {
    event.stopPropagation();
    if (field === 'percentComplete' && task.status === 'Completed') return;
    this.editingTaskId = task.id;
    this.editingField = field;
    switch (field) {
      case 'name':
        this.editingValue = task.name;
        break;
      case 'status':
        this.editingValue = task.status;
        break;
      case 'plannedStart':
        this.editingValue = task.plannedStart ? this.toDateInputValue(task.plannedStart) : '';
        break;
      case 'plannedEnd':
        this.editingValue = task.plannedEnd ? this.toDateInputValue(task.plannedEnd) : '';
        break;
      case 'percentComplete':
        this.editingValue = String(task.percentComplete ?? 0);
        break;
    }
    this.cdr.markForCheck();
  }

  commitEdit(task: GanttTask): void {
    if (!this.editingField) return;
    const field = this.editingField;
    const value = this.editingValue;
    this.cancelEdit();

    let emitValue: string | number | null = value;
    if (field === 'percentComplete') {
      const n = Number(value);
      emitValue = isNaN(n) ? task.percentComplete : Math.max(0, Math.min(100, n));
    } else if ((field === 'plannedStart' || field === 'plannedEnd') && !value) {
      emitValue = null;
    }

    this.inlineEdit.emit({ taskId: task.id, version: task.version, field, value: emitValue });
  }

  cancelEdit(): void {
    this.editingTaskId = null;
    this.editingField = null;
    this.editingValue = '';
    this.cdr.markForCheck();
  }

  onEditKeydown(event: KeyboardEvent, task: GanttTask): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.commitEdit(task);
    }
    if (event.key === 'Escape') {
      this.cancelEdit();
    }
  }

  stopPropagation(event: Event): void {
    event.stopPropagation();
  }

  private toDateInputValue(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
