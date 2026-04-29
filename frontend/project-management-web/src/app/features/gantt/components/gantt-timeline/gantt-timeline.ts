import {
  Component, Input, OnChanges, SimpleChanges, ChangeDetectionStrategy,
  AfterViewChecked, Output, EventEmitter, OnDestroy, ViewChild, ElementRef,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import {
  GanttConfig, GanttGranularity, GanttTask, GanttTaskEdit,
  DEFAULT_GANTT_CONFIG,
} from '../../models/gantt.model';
import { GanttTimelineService, TimelineRange, HeaderCell } from '../../services/gantt-timeline.service';

interface DragState {
  task: GanttTask;
  startClientX: number;
  originalStart: Date;
  originalEnd: Date;
  mode: 'move' | 'resize';
}

@Component({
  selector: 'app-gantt-timeline',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './gantt-timeline.html',
  styleUrl: './gantt-timeline.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GanttTimelineComponent implements OnChanges, AfterViewChecked, OnDestroy {
  @Input() tasks: GanttTask[] = [];
  @Input() granularity: GanttGranularity = 'week';
  @Input() scrollTop = 0;
  @Input() config: GanttConfig = DEFAULT_GANTT_CONFIG;

  @Output() scrollChange = new EventEmitter<number>();
  @Output() taskEdited = new EventEmitter<GanttTaskEdit>();

  @ViewChild('svgEl') svgRef!: ElementRef<SVGSVGElement>;

  range: TimelineRange = { start: new Date(), end: new Date(), totalDays: 0, totalWidth: 0 };
  monthHeaders: HeaderCell[] = [];
  weekOrDayHeaders: HeaderCell[] = [];
  visibleTasks: GanttTask[] = [];
  todayX = 0;

  ghostTask: GanttTask | null = null;

  readonly barHeight = 22;
  private renderStart = 0;
  private needsRender = false;
  private dragging: DragState | null = null;
  private rafPending = false;

  private readonly onMouseMove = (e: MouseEvent) => this.handleMouseMove(e);
  private readonly onMouseUp = (e: MouseEvent) => this.handleMouseUp(e);

  constructor(
    private readonly timeline: GanttTimelineService,
    private readonly cdr: ChangeDetectorRef,
  ) {}

  ngOnChanges(_changes: SimpleChanges): void {
    this.needsRender = true;
    this.recalculate();
  }

  ngAfterViewChecked(): void {
    if (this.needsRender) {
      this.needsRender = false;
    }
  }

  ngOnDestroy(): void {
    this.removeDragListeners();
  }

  private recalculate(): void {
    this.renderStart = performance.now();
    const ppd = this.pixelsPerDay;
    const effectiveConfig = { ...this.config, pixelsPerDay: ppd };

    this.range = this.timeline.getTimelineRange(this.tasks, effectiveConfig);
    this.monthHeaders = this.timeline.getMonthHeaders(this.range, ppd);
    this.weekOrDayHeaders = this.granularity === 'week'
      ? this.timeline.getWeekHeaders(this.range, ppd)
      : this.timeline.getDayHeaders(this.range, ppd);

    this.visibleTasks = this.getVisibleTasks();
    this.todayX = this.timeline.dateToX(new Date(), this.range.start, ppd);

    const elapsed = performance.now() - this.renderStart;
    if (elapsed > 2000) {
      console.warn(`[Gantt] Render time exceeded 2s: ${elapsed.toFixed(0)}ms for ${this.tasks.length} tasks`);
    }
  }

  private getVisibleTasks(): GanttTask[] {
    const taskMap = new Map(this.tasks.map(t => [t.id, t]));
    return this.tasks.filter(task => {
      let parentId = task.parentId;
      while (parentId) {
        const parent = taskMap.get(parentId);
        if (!parent) break;
        if (parent.collapsed) return false;
        parentId = parent.parentId;
      }
      return true;
    });
  }

  // ─── Drag / Resize ────────────────────────────────────────────────────────

  onBarMouseDown(event: MouseEvent, task: GanttTask, mode: 'move' | 'resize'): void {
    if (!task.plannedStart || !task.plannedEnd) return;
    event.preventDefault();
    event.stopPropagation();

    this.dragging = {
      task,
      startClientX: event.clientX,
      originalStart: new Date(task.plannedStart),
      originalEnd: new Date(task.plannedEnd),
      mode,
    };
    this.ghostTask = { ...task };

    document.addEventListener('mousemove', this.onMouseMove);
    document.addEventListener('mouseup', this.onMouseUp);
  }

  private handleMouseMove(event: MouseEvent): void {
    if (!this.dragging) return;
    const deltaX = event.clientX - this.dragging.startClientX;
    const deltaDays = Math.round(deltaX / this.pixelsPerDay);

    if (this.dragging.mode === 'move') {
      this.ghostTask = {
        ...this.dragging.task,
        plannedStart: addDays(this.dragging.originalStart, deltaDays),
        plannedEnd: addDays(this.dragging.originalEnd, deltaDays),
      };
    } else {
      const minEnd = addDays(this.dragging.originalStart, 1);
      const newEnd = addDays(this.dragging.originalEnd, deltaDays);
      this.ghostTask = {
        ...this.dragging.task,
        plannedEnd: newEnd > minEnd ? newEnd : minEnd,
      };
    }
    if (!this.rafPending) {
      this.rafPending = true;
      requestAnimationFrame(() => {
        this.rafPending = false;
        this.cdr.detectChanges();
      });
    }
  }

  private handleMouseUp(event: MouseEvent): void {
    if (!this.dragging) return;

    const deltaX = event.clientX - this.dragging.startClientX;
    const deltaDays = Math.round(deltaX / this.pixelsPerDay);

    if (deltaDays !== 0) {
      const edit: GanttTaskEdit = {
        taskId: this.dragging.task.id,
        originalVersion: this.dragging.task.version,
      };

      if (this.dragging.mode === 'move') {
        edit.newPlannedStart = addDays(this.dragging.originalStart, deltaDays);
        edit.newPlannedEnd = addDays(this.dragging.originalEnd, deltaDays);
      } else {
        const minEnd = addDays(this.dragging.originalStart, 1);
        const newEnd = addDays(this.dragging.originalEnd, deltaDays);
        edit.newPlannedEnd = newEnd > minEnd ? newEnd : minEnd;
      }

      this.taskEdited.emit(edit);
    }

    this.dragging = null;
    this.ghostTask = null;
    this.removeDragListeners();
    this.cdr.detectChanges();
  }

  private removeDragListeners(): void {
    document.removeEventListener('mousemove', this.onMouseMove);
    document.removeEventListener('mouseup', this.onMouseUp);
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────

  get totalWidth(): number {
    return this.range.totalWidth;
  }

  get totalHeight(): number {
    return this.config.headerHeight + this.visibleTasks.length * this.config.rowHeight;
  }

  get pixelsPerDay(): number {
    return this.granularity === 'week'
      ? this.config.pixelsPerWeek / 7
      : this.config.pixelsPerDay;
  }

  dateToX(date: Date | null): number {
    if (!date) return 0;
    return this.timeline.dateToX(date, this.range.start, this.pixelsPerDay);
  }

  getBarWidth(task: GanttTask): number {
    if (!task.plannedStart || !task.plannedEnd) return 0;
    return Math.max(this.dateToX(task.plannedEnd) - this.dateToX(task.plannedStart), 4);
  }

  getBarColor(task: GanttTask): string {
    return this.timeline.getBarColor(task);
  }

  getMilestoneDiamond(task: GanttTask): string {
    return this.timeline.getMilestoneDiamond(
      task, this.range.start, this.pixelsPerDay, this.config.rowHeight
    );
  }

  getRowY(index: number): number {
    return index * this.config.rowHeight + this.config.rowHeight / 2;
  }

  onScroll(event: Event): void {
    const el = event.target as HTMLElement;
    this.scrollChange.emit(el.scrollTop);
  }

  trackTask(_index: number, task: GanttTask): string {
    return task.id;
  }
}

function addDays(date: Date, days: number): Date {
  const d = new Date(date);
  d.setDate(d.getDate() + days);
  return d;
}
