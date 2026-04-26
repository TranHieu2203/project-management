import {
  ChangeDetectionStrategy, ChangeDetectorRef, Component,
  EventEmitter, Input, Output, inject, signal,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ProjectTask } from '../../models/task.model';
import { ProjectMember } from '../../models/project.model';
import { DeadlineAlertService, DeadlineStatus } from '../../services/deadline-alert.service';

export interface TaskReorderEvent {
  taskId: string;
  newSortOrder: number;
  newParentId: string | null;
  version: number;
}

export interface QuickUpdateEvent {
  task: ProjectTask;
  field: EditableField;
  value: string | number | null;
}

export interface ColumnDef {
  key: string;
  label: string;
  defaultVisible: boolean;
}

type EditableField =
  | 'name' | 'status' | 'priority' | 'assigneeUserId' | 'percentComplete'
  | 'plannedStartDate' | 'plannedEndDate' | 'actualStartDate' | 'actualEndDate';

interface EditingCell {
  taskId: string;
  field: EditableField;
}

interface FlatNode {
  task: ProjectTask;
  depth: number;
  isLast: boolean;
  ancestorIsLast: boolean[];
}

const LS_KEY = 'task-tree-columns-v1';

@Component({
  standalone: true,
  selector: 'app-task-tree',
  imports: [NgClass, FormsModule, MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './task-tree.html',
  styleUrl: './task-tree.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskTreeComponent {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly deadlineService = inject(DeadlineAlertService);

  @Input() set tasks(tasks: ProjectTask[]) {
    this._flatNodes = this.buildFlatNodes(tasks);
  }

  @Input() set members(members: ProjectMember[]) {
    this._membersMap = new Map(members.map(m => [m.userId, m]));
    this._membersList = members;
  }

  @Input() today: string = this.deadlineService.getLocalDateString();
  @Input() activeDeadlineFilter: DeadlineStatus | null = null;
  @Input() highlightTaskId: string | null = null;

  @Output() addChild = new EventEmitter<string>();
  @Output() editTask = new EventEmitter<ProjectTask>();
  @Output() deleteTask = new EventEmitter<ProjectTask>();
  @Output() reorderTask = new EventEmitter<TaskReorderEvent>();
  @Output() quickUpdateTask = new EventEmitter<QuickUpdateEvent>();

  _flatNodes: FlatNode[] = [];
  _membersMap = new Map<string, ProjectMember>();
  _membersList: ProjectMember[] = [];

  // ── Column visibility ────────────────────────────────────────────────────────

  readonly COLUMNS: ColumnDef[] = [
    { key: 'type',         label: 'Loại',            defaultVisible: true  },
    { key: 'vbs',          label: 'Mã',              defaultVisible: true  },
    { key: 'priority',     label: 'Ưu tiên',          defaultVisible: true  },
    { key: 'status',       label: 'Trạng thái',       defaultVisible: true  },
    { key: 'plannedStart', label: 'KH Bắt đầu',      defaultVisible: true  },
    { key: 'plannedEnd',   label: 'KH Kết thúc',     defaultVisible: true  },
    { key: 'actualStart',  label: 'TT Bắt đầu',      defaultVisible: false },
    { key: 'actualEnd',    label: 'TT Kết thúc',     defaultVisible: false },
    { key: 'percent',      label: '% Hoàn thành',    defaultVisible: true  },
    { key: 'assignee',     label: 'Người thực hiện', defaultVisible: true  },
  ];

  visibleCols: Set<string>;
  readonly colPickerOpen = signal(false);

  constructor() {
    this.visibleCols = this.loadColVisibility();
  }

  private loadColVisibility(): Set<string> {
    try {
      const saved = localStorage.getItem(LS_KEY);
      if (saved) {
        const parsed: Record<string, boolean> = JSON.parse(saved);
        return new Set(this.COLUMNS.filter(c => parsed[c.key] !== false).map(c => c.key));
      }
    } catch { /* use defaults */ }
    return new Set(this.COLUMNS.filter(c => c.defaultVisible).map(c => c.key));
  }

  private saveColVisibility(): void {
    const obj: Record<string, boolean> = {};
    this.COLUMNS.forEach(c => { obj[c.key] = this.visibleCols.has(c.key); });
    try { localStorage.setItem(LS_KEY, JSON.stringify(obj)); } catch { /* ignore */ }
  }

  toggleCol(key: string): void {
    const next = new Set(this.visibleCols);
    next.has(key) ? next.delete(key) : next.add(key);
    this.visibleCols = next;
    this.saveColVisibility();
    this.cdr.markForCheck();
  }

  toggleColPicker(): void {
    this.colPickerOpen.set(!this.colPickerOpen());
  }

  get gridCols(): string {
    const c = this.visibleCols;
    const cols: string[] = ['20px'];
    if (c.has('type'))         cols.push('84px');
    if (c.has('vbs'))          cols.push('52px');
    cols.push('1fr');
    if (c.has('priority'))     cols.push('100px');
    if (c.has('status'))       cols.push('148px');
    if (c.has('plannedStart')) cols.push('88px');
    if (c.has('plannedEnd'))   cols.push('88px');
    if (c.has('actualStart'))  cols.push('88px');
    if (c.has('actualEnd'))    cols.push('88px');
    if (c.has('percent'))      cols.push('52px');
    if (c.has('assignee'))     cols.push('1fr');
    cols.push('96px');
    return cols.join(' ');
  }

  // ── Inline edit state ────────────────────────────────────────────────────────

  editingCell: EditingCell | null = null;
  editingValue: string | number | null = null;

  readonly statuses: ProjectTask['status'][] = [
    'NotStarted', 'InProgress', 'Completed', 'OnHold', 'Cancelled', 'Delayed',
  ];
  readonly priorities: ProjectTask['priority'][] = ['Low', 'Medium', 'High', 'Critical'];

  // ── Drag state ──────────────────────────────────────────────────────────────

  draggedIdx: number | null = null;
  dragOverIdx: number | null = null;
  dropMode: 'above' | 'below' | 'into' = 'below';

  // ── Tree building ────────────────────────────────────────────────────────────

  private buildFlatNodes(tasks: ProjectTask[]): FlatNode[] {
    const result: FlatNode[] = [];
    const byParent = new Map<string | null, ProjectTask[]>();

    for (const t of tasks) {
      const key = t.parentId ?? null;
      if (!byParent.has(key)) byParent.set(key, []);
      byParent.get(key)!.push(t);
    }

    const traverse = (parentId: string | null, depth: number, ancestorIsLast: boolean[]) => {
      const children = (byParent.get(parentId) ?? []).sort((a, b) => a.sortOrder - b.sortOrder);
      children.forEach((child, idx) => {
        const isLast = idx === children.length - 1;
        result.push({ task: child, depth, isLast, ancestorIsLast: [...ancestorIsLast] });
        traverse(child.id, depth + 1, [...ancestorIsLast, isLast]);
      });
    };

    traverse(null, 0, []);
    return result;
  }

  // ── Inline edit methods ──────────────────────────────────────────────────────

  isEditing(taskId: string, field: EditableField): boolean {
    return this.editingCell?.taskId === taskId && this.editingCell?.field === field;
  }

  isRowEditing(taskId: string): boolean {
    return this.editingCell?.taskId === taskId;
  }

  startEdit(task: ProjectTask, field: EditableField, event: Event): void {
    event.stopPropagation();
    if (field === 'percentComplete' && task.status === 'Completed') return;
    this.editingCell = { taskId: task.id, field };
    const raw = task[field] as string | number | null;
    this.editingValue = raw ?? '';
    this.cdr.markForCheck();
  }

  commitEdit(task: ProjectTask): void {
    if (!this.editingCell || this.editingCell.taskId !== task.id) return;
    const field = this.editingCell.field;
    const newValue = this.editingValue;
    const originalValue = (task[field] as string | number | null) ?? '';

    this.editingCell = null;
    this.editingValue = null;
    this.cdr.markForCheck();

    if (newValue === originalValue) return;

    this.quickUpdateTask.emit({ task, field, value: newValue === '' ? null : newValue });
  }

  cancelEdit(): void {
    this.editingCell = null;
    this.editingValue = null;
    this.cdr.markForCheck();
  }

  onEditKeydown(event: KeyboardEvent, task: ProjectTask): void {
    if (event.key === 'Enter') { event.preventDefault(); this.commitEdit(task); }
    if (event.key === 'Escape') { this.cancelEdit(); }
  }

  stopPropagation(event: Event): void {
    event.stopPropagation();
  }

  // ── Drag events ─────────────────────────────────────────────────────────────

  onDragStart(event: DragEvent, idx: number): void {
    this.draggedIdx = idx;
    event.dataTransfer!.effectAllowed = 'move';
    event.dataTransfer!.setData('text/plain', String(idx));
  }

  onDragOver(event: DragEvent, idx: number): void {
    if (this.draggedIdx === null || this.draggedIdx === idx) return;
    event.preventDefault();
    event.dataTransfer!.dropEffect = 'move';
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const ratio = (event.clientY - rect.top) / rect.height;
    const target = this._flatNodes[idx];
    const dragged = this._flatNodes[this.draggedIdx];
    const wouldCycle = this.isDescendantOf(dragged.task.id, target.task.id);
    // If drop-into would create cycle, fall back to sibling modes
    const zoneMode: 'above' | 'below' | 'into' = ratio < 0.3 ? 'above' : ratio > 0.7 ? 'below' : 'into';
    this.dropMode = (zoneMode === 'into' && wouldCycle) ? 'below' : zoneMode;
    this.dragOverIdx = idx;
    this.cdr.markForCheck();
  }

  onDragLeave(event: DragEvent): void {
    const related = event.relatedTarget as HTMLElement | null;
    if (related?.closest('.task-row')) return;
    this.dragOverIdx = null;
    this.cdr.markForCheck();
  }

  private isDescendantOf(ancestorId: string, nodeId: string): boolean {
    let current: string | null = nodeId;
    const nodeMap = new Map(this._flatNodes.map(n => [n.task.id, n.task]));
    while (current) {
      if (current === ancestorId) return true;
      current = nodeMap.get(current)?.parentId ?? null;
    }
    return false;
  }

  onDrop(event: DragEvent, targetIdx: number): void {
    event.preventDefault();
    if (this.draggedIdx === null || this.draggedIdx === targetIdx) {
      this.clearDrag();
      return;
    }

    const dragged = this._flatNodes[this.draggedIdx];
    const target = this._flatNodes[targetIdx];

    // Block moves that would create a cycle (dragging ancestor into descendant)
    if (this.isDescendantOf(dragged.task.id, target.task.id)) {
      this.clearDrag();
      return;
    }

    if (this.dropMode === 'into') {
      const targetChildren = this._flatNodes
        .filter(n => n.task.parentId === target.task.id)
        .sort((a, b) => a.task.sortOrder - b.task.sortOrder);
      const last = targetChildren[targetChildren.length - 1];
      const newSortOrder = last ? last.task.sortOrder + 1000 : 1000;
      this.reorderTask.emit({
        taskId: dragged.task.id,
        newSortOrder,
        newParentId: target.task.id,
        version: dragged.task.version,
      });
    } else {
      const siblings = this._flatNodes
        .filter(n => n.task.parentId === target.task.parentId)
        .sort((a, b) => a.task.sortOrder - b.task.sortOrder);
      const tIdx = siblings.findIndex(s => s.task.id === target.task.id);
      let newSortOrder: number;
      if (this.dropMode === 'above') {
        const prev = siblings[tIdx - 1];
        newSortOrder = prev
          ? (prev.task.sortOrder + target.task.sortOrder) / 2
          : target.task.sortOrder - 1000;
      } else {
        const next = siblings[tIdx + 1];
        newSortOrder = next
          ? (target.task.sortOrder + next.task.sortOrder) / 2
          : target.task.sortOrder + 1000;
      }
      this.reorderTask.emit({
        taskId: dragged.task.id,
        newSortOrder: Math.round(newSortOrder),
        newParentId: target.task.parentId,
        version: dragged.task.version,
      });
    }

    this.clearDrag();
  }

  onDragEnd(): void {
    this.clearDrag();
  }

  private clearDrag(): void {
    this.draggedIdx = null;
    this.dragOverIdx = null;
    this.cdr.markForCheck();
  }

  // ── Helpers ──────────────────────────────────────────────────────────────────

  assigneeLabel(userId: string | null): string {
    if (!userId) return '';
    const m = this._membersMap.get(userId);
    if (!m) return userId.substring(0, 8) + '...';
    return m.displayName ?? m.username;
  }

  priorityClass(priority: string): string {
    return 'priority-' + priority.toLowerCase();
  }

  statusClass(status: string): string {
    return 'status-' + status.toLowerCase();
  }

  statusLabel(status: string): string {
    const labels: Record<string, string> = {
      NotStarted: 'Chưa bắt đầu', InProgress: 'Đang làm',
      Completed: 'Hoàn thành', OnHold: 'Tạm dừng',
      Cancelled: 'Đã hủy', Delayed: 'Chậm tiến độ',
    };
    return labels[status] ?? status;
  }

  formatDate(date: string | null): string {
    if (!date) return '—';
    const [y, m, d] = date.split('-');
    return `${d}/${m}/${y.slice(2)}`;
  }

  rowClasses(task: ProjectTask): Record<string, boolean> {
    const s = this.deadlineService.getDeadlineStatus(task, this.today);
    return {
      'row-overdue':   s === 'overdue',
      'row-due-today': s === 'due-today',
      'row-due-soon':  s === 'due-soon',
      'row-filtered':  !!this.activeDeadlineFilter && s === this.activeDeadlineFilter,
      'row-highlight': task.id === this.highlightTaskId,
    };
  }
}
