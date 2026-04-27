import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  inject,
  Input,
  OnChanges,
  OnDestroy,
  OnInit,
  Output,
  signal,
  SimpleChanges,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { FilterCriteria, FilterPreset } from '../../models/filter.model';
import {
  countActiveFilters,
  getChipLabel,
  isEmpty,
  removeCriterion,
  serializeFilter,
} from '../../models/filter.utils';
import { FilterPresetsService } from '../../services/filter-presets.service';
import { ProjectMember } from '../../models/project.model';
import { ProjectTask } from '../../models/task.model';

export interface ActiveChip {
  key: keyof FilterCriteria;
  label: string;
}

@Component({
  standalone: true,
  selector: 'app-filter-bar',
  imports: [FormsModule, NgClass, MatIconModule, MatButtonModule, MatTooltipModule],
  templateUrl: './filter-bar.html',
  styleUrl: './filter-bar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilterBarComponent implements OnInit, OnChanges, OnDestroy {
  private readonly presetsService = inject(FilterPresetsService);
  private readonly destroy$ = new Subject<void>();
  private readonly keywordSubject = new Subject<string>();

  @Input() criteria: FilterCriteria = {};
  @Input() members: ProjectMember[] = [];
  @Input() tasks: ProjectTask[] = [];
  @Input() totalCount = 0;
  @Input() filteredCount = 0;
  @Input() showGanttToggle = false;

  @Output() criteriaChange = new EventEmitter<FilterCriteria>();

  // UI state
  readonly advancedOpen = signal(false);
  readonly presetsOpen = signal(false);
  readonly saveNameInput = signal('');
  readonly savingPreset = signal(false);

  // Dropdown state
  statusOptions: ProjectTask['status'][] = [
    'NotStarted', 'InProgress', 'OnHold', 'Delayed', 'Completed', 'Cancelled',
  ];
  priorityOptions: ProjectTask['priority'][] = ['Low', 'Medium', 'High', 'Critical'];
  nodeTypeOptions: ProjectTask['type'][] = ['Phase', 'Milestone', 'Task'];

  get milestones(): ProjectTask[] {
    return this.tasks.filter(t => t.type === 'Milestone');
  }

  get activeChips(): ActiveChip[] {
    const chips: ActiveChip[] = [];
    const c = this.criteria;
    if (c.keyword) chips.push({ key: 'keyword', label: getChipLabel('keyword', c) });
    if (c.statuses?.length) chips.push({ key: 'statuses', label: getChipLabel('statuses', c) });
    if (c.assigneeIds?.length) chips.push({ key: 'assigneeIds', label: getChipLabel('assigneeIds', c) });
    if (c.priorities?.length) chips.push({ key: 'priorities', label: getChipLabel('priorities', c) });
    if (c.nodeTypes?.length) chips.push({ key: 'nodeTypes', label: getChipLabel('nodeTypes', c) });
    if (c.milestoneId) chips.push({ key: 'milestoneId', label: getChipLabel('milestoneId', c) });
    if (c.dueDateFrom || c.dueDateTo) chips.push({ key: 'dueDateFrom', label: getChipLabel('dueDateFrom', c) });
    if (c.overdueOnly) chips.push({ key: 'overdueOnly', label: getChipLabel('overdueOnly', c) });
    return chips;
  }

  get activeCount(): number {
    return countActiveFilters(this.criteria);
  }

  get hasFilter(): boolean {
    return !isEmpty(this.criteria);
  }

  get presets(): FilterPreset[] {
    return this.presetsService.getAll();
  }

  // Quick preset states
  get isMyTasksActive(): boolean {
    return !!this.criteria.assigneeIds?.includes('CURRENT_USER');
  }
  get isOverdueActive(): boolean {
    return !!this.criteria.overdueOnly;
  }
  get isHighPriorityActive(): boolean {
    const p = this.criteria.priorities;
    return !!(p?.includes('High') || p?.includes('Critical'));
  }
  get isUnassignedActive(): boolean {
    return !!this.criteria.assigneeIds?.includes('UNASSIGNED');
  }

  // Advanced filter form state — bound to dropdowns
  advStatus: Record<string, boolean> = {};
  advPriority: Record<string, boolean> = {};
  advNodeType: Record<string, boolean> = {};
  advAssigneeId = '';
  advDueDateFrom = '';
  advDueDateTo = '';
  advMilestoneId = '';

  ngOnInit(): void {
    // Debounce keyword input
    this.keywordSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$),
    ).subscribe(keyword => {
      this.emit({ ...this.criteria, keyword: keyword || undefined });
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['criteria']) {
      this.syncAdvancedState();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Keyword ──────────────────────────────────────────────────────────────────

  onKeywordInput(value: string): void {
    this.keywordSubject.next(value);
  }

  // ── Quick Presets ────────────────────────────────────────────────────────────

  toggleMyTasks(): void {
    const ids = this.criteria.assigneeIds ?? [];
    if (ids.includes('CURRENT_USER')) {
      this.emit({ ...this.criteria, assigneeIds: ids.filter(i => i !== 'CURRENT_USER') || undefined });
    } else {
      this.emit({ ...this.criteria, assigneeIds: [...ids, 'CURRENT_USER'] });
    }
  }

  toggleOverdue(): void {
    this.emit({ ...this.criteria, overdueOnly: !this.criteria.overdueOnly || undefined });
  }

  toggleHighPriority(): void {
    const current = this.criteria.priorities ?? [];
    const highAndCritical = ['High', 'Critical'] as const;
    const allActive = highAndCritical.every(p => current.includes(p as any));
    if (allActive) {
      const next = current.filter(p => p !== 'High' && p !== 'Critical');
      this.emit({ ...this.criteria, priorities: next.length ? next : undefined });
    } else {
      const next = [...new Set([...current, 'High' as const, 'Critical' as const])];
      this.emit({ ...this.criteria, priorities: next });
    }
  }

  toggleUnassigned(): void {
    const ids = this.criteria.assigneeIds ?? [];
    if (ids.includes('UNASSIGNED')) {
      this.emit({ ...this.criteria, assigneeIds: ids.filter(i => i !== 'UNASSIGNED') || undefined });
    } else {
      this.emit({ ...this.criteria, assigneeIds: [...ids, 'UNASSIGNED'] });
    }
  }

  // ── Active Chips ─────────────────────────────────────────────────────────────

  removeChip(key: keyof FilterCriteria): void {
    // dueDateFrom chip covers both from+to
    if (key === 'dueDateFrom') {
      const next = { ...this.criteria };
      delete next.dueDateFrom;
      delete next.dueDateTo;
      this.emit(next);
    } else {
      this.emit(removeCriterion(this.criteria, key));
    }
  }

  clearAll(): void {
    this.emit({});
  }

  // ── Advanced Dropdown ────────────────────────────────────────────────────────

  toggleAdvanced(): void {
    this.advancedOpen.set(!this.advancedOpen());
    if (this.advancedOpen()) this.presetsOpen.set(false);
  }

  toggleAdvStatus(s: string): void {
    this.advStatus = { ...this.advStatus, [s]: !this.advStatus[s] };
    this.applyAdvanced();
  }

  toggleAdvPriority(p: string): void {
    this.advPriority = { ...this.advPriority, [p]: !this.advPriority[p] };
    this.applyAdvanced();
  }

  toggleAdvNodeType(t: string): void {
    this.advNodeType = { ...this.advNodeType, [t]: !this.advNodeType[t] };
    this.applyAdvanced();
  }

  onAdvAssigneeChange(): void {
    this.applyAdvanced();
  }

  onAdvMilestoneChange(): void {
    this.applyAdvanced();
  }

  onAdvDateChange(): void {
    // Apply only when both dates are filled OR both cleared
    if ((this.advDueDateFrom && this.advDueDateTo) ||
        (!this.advDueDateFrom && !this.advDueDateTo)) {
      this.applyAdvanced();
    }
  }

  private applyAdvanced(): void {
    const statuses = this.statusOptions.filter(s => this.advStatus[s]) as any[];
    const priorities = this.priorityOptions.filter(p => this.advPriority[p]) as any[];
    const nodeTypes = this.nodeTypeOptions.filter(t => this.advNodeType[t]) as any[];

    // Merge with quick preset assigneeIds (CURRENT_USER / UNASSIGNED) but NOT with advAssigneeId
    const quickAssignees = (this.criteria.assigneeIds ?? []).filter(
      id => id === 'CURRENT_USER' || id === 'UNASSIGNED'
    );
    const advAssignees = this.advAssigneeId ? [this.advAssigneeId] : [];
    const assigneeIds = [...new Set([...quickAssignees, ...advAssignees])];

    this.emit({
      ...this.criteria,
      statuses: statuses.length ? statuses : undefined,
      priorities: priorities.length ? priorities : undefined,
      nodeTypes: nodeTypes.length ? nodeTypes : undefined,
      assigneeIds: assigneeIds.length ? assigneeIds : undefined,
      milestoneId: this.advMilestoneId || undefined,
      dueDateFrom: this.advDueDateFrom || undefined,
      dueDateTo: this.advDueDateTo || undefined,
    });
  }

  private syncAdvancedState(): void {
    const c = this.criteria;
    this.advStatus = {};
    (c.statuses ?? []).forEach(s => this.advStatus[s] = true);
    this.advPriority = {};
    (c.priorities ?? []).forEach(p => this.advPriority[p] = true);
    this.advNodeType = {};
    (c.nodeTypes ?? []).forEach(t => this.advNodeType[t] = true);
    const memberIds = (c.assigneeIds ?? []).filter(
      id => id !== 'CURRENT_USER' && id !== 'UNASSIGNED'
    );
    this.advAssigneeId = memberIds[0] ?? '';
    this.advMilestoneId = c.milestoneId ?? '';
    this.advDueDateFrom = c.dueDateFrom ?? '';
    this.advDueDateTo = c.dueDateTo ?? '';
  }

  // ── Saved Presets ────────────────────────────────────────────────────────────

  togglePresets(): void {
    this.presetsOpen.set(!this.presetsOpen());
    if (this.presetsOpen()) this.advancedOpen.set(false);
  }

  applyPreset(preset: FilterPreset): void {
    this.emit({ ...preset.criteria });
    this.presetsOpen.set(false);
  }

  deletePreset(event: MouseEvent, id: string): void {
    event.stopPropagation();
    this.presetsService.delete(id);
  }

  startSavePreset(): void {
    this.savingPreset.set(true);
    this.saveNameInput.set('');
  }

  confirmSavePreset(): void {
    const name = this.saveNameInput().trim();
    if (!name) return;
    this.presetsService.save({
      id: this.presetsService.generateId(),
      name,
      criteria: { ...this.criteria },
    });
    this.savingPreset.set(false);
    this.saveNameInput.set('');
  }

  cancelSavePreset(): void {
    this.savingPreset.set(false);
  }

  getMemberName(userId: string): string {
    return this.members.find(m => m.userId === userId)?.displayName ?? userId;
  }

  private emit(criteria: FilterCriteria): void {
    this.criteriaChange.emit(criteria);
  }

  // Status label map
  readonly statusLabels: Record<string, string> = {
    NotStarted: 'Chưa bắt đầu',
    InProgress: 'Đang thực hiện',
    OnHold: 'Tạm dừng',
    Delayed: 'Bị trễ',
    Completed: 'Hoàn thành',
    Cancelled: 'Đã hủy',
  };

  readonly priorityLabels: Record<string, string> = {
    Low: 'Thấp',
    Medium: 'Trung bình',
    High: 'Cao',
    Critical: 'Khẩn cấp',
  };

  readonly nodeTypeLabels: Record<string, string> = {
    Phase: 'Phase',
    Milestone: 'Milestone',
    Task: 'Task',
  };
}
