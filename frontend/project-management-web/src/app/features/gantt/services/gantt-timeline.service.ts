import { Injectable } from '@angular/core';
import { GanttConfig, GanttGranularity, GanttTask } from '../models/gantt.model';

export interface TimelineRange {
  start: Date;
  end: Date;
  totalDays: number;
  totalWidth: number;
}

export interface HeaderCell {
  label: string;
  x: number;
  width: number;
}

@Injectable({ providedIn: 'root' })
export class GanttTimelineService {

  getTimelineRange(tasks: GanttTask[], config: GanttConfig): TimelineRange {
    const dates: Date[] = [];
    for (const t of tasks) {
      if (t.plannedStart) dates.push(t.plannedStart);
      if (t.plannedEnd) dates.push(t.plannedEnd);
    }

    let start: Date;
    let end: Date;

    if (dates.length === 0) {
      start = this.startOfWeek(new Date());
      end = this.addDays(start, 84); // 12 weeks
    } else {
      const minMs = Math.min(...dates.map(d => d.getTime()));
      const maxMs = Math.max(...dates.map(d => d.getTime()));
      start = this.startOfWeek(new Date(minMs));
      end = this.addDays(this.endOfWeek(new Date(maxMs)), 14); // +2 weeks buffer
    }

    const totalDays = Math.ceil((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24));
    const totalWidth = totalDays * config.pixelsPerDay;

    return { start, end, totalDays, totalWidth };
  }

  dateToX(date: Date, timelineStart: Date, pixelsPerDay: number): number {
    const diffMs = date.getTime() - timelineStart.getTime();
    const diffDays = diffMs / (1000 * 60 * 60 * 24);
    return Math.round(diffDays * pixelsPerDay);
  }

  getMonthHeaders(range: TimelineRange, pixelsPerDay: number): HeaderCell[] {
    const headers: HeaderCell[] = [];
    const current = new Date(range.start.getFullYear(), range.start.getMonth(), 1);

    while (current < range.end) {
      const monthStart = new Date(current);
      const monthEnd = new Date(current.getFullYear(), current.getMonth() + 1, 0);
      const clampedStart = monthStart < range.start ? range.start : monthStart;
      const clampedEnd = monthEnd > range.end ? range.end : monthEnd;

      const x = this.dateToX(clampedStart, range.start, pixelsPerDay);
      const endX = this.dateToX(new Date(clampedEnd.getTime() + 86400000), range.start, pixelsPerDay);

      headers.push({
        label: current.toLocaleDateString('vi-VN', { month: 'long', year: 'numeric' }),
        x,
        width: endX - x,
      });

      current.setMonth(current.getMonth() + 1);
    }
    return headers;
  }

  getWeekHeaders(range: TimelineRange, pixelsPerDay: number): HeaderCell[] {
    const headers: HeaderCell[] = [];
    const current = new Date(range.start);

    while (current < range.end) {
      const weekStart = new Date(current);
      const weekNum = this.getWeekNumber(weekStart);
      const x = this.dateToX(weekStart, range.start, pixelsPerDay);

      headers.push({
        label: `T${weekNum}`,
        x,
        width: pixelsPerDay * 7,
      });

      current.setDate(current.getDate() + 7);
    }
    return headers;
  }

  getDayHeaders(range: TimelineRange, pixelsPerDay: number): HeaderCell[] {
    const headers: HeaderCell[] = [];
    const current = new Date(range.start);

    while (current < range.end) {
      const x = this.dateToX(current, range.start, pixelsPerDay);
      headers.push({
        label: current.getDate().toString(),
        x,
        width: pixelsPerDay,
      });
      current.setDate(current.getDate() + 1);
    }
    return headers;
  }

  private startOfWeek(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1); // Monday
    d.setDate(diff);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  private endOfWeek(date: Date): Date {
    const d = this.startOfWeek(date);
    d.setDate(d.getDate() + 6);
    return d;
  }

  private addDays(date: Date, days: number): Date {
    const d = new Date(date);
    d.setDate(d.getDate() + days);
    return d;
  }

  private getWeekNumber(date: Date): number {
    const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
    const dayNum = d.getUTCDay() || 7;
    d.setUTCDate(d.getUTCDate() + 4 - dayNum);
    const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
    return Math.ceil((((d.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
  }

  getBarColor(task: GanttTask): string {
    if (task.type === 'Phase') return '#2196F3';
    if (task.type === 'Milestone') return '#FF9800';
    if (task.status === 'Delayed') return '#F44336';
    if (task.status === 'Completed') return '#9E9E9E';
    return '#4CAF50';
  }

  getMilestoneDiamond(
    task: GanttTask,
    timelineStart: Date,
    pixelsPerDay: number,
    rowHeight: number,
  ): string {
    const date = task.plannedStart ?? task.plannedEnd;
    if (!date) return '';
    const cx = this.dateToX(date, timelineStart, pixelsPerDay);
    const cy = rowHeight / 2;
    const r = 8;
    return `${cx},${cy - r} ${cx + r},${cy} ${cx},${cy + r} ${cx - r},${cy}`;
  }

  calculateArrowPath(
    from: GanttTask,
    to: GanttTask,
    type: 'FS' | 'SS' | 'FF' | 'SF',
    fromRowIndex: number,
    toRowIndex: number,
    timelineStart: Date,
    pixelsPerDay: number,
    rowHeight: number,
  ): string {
    const fromMidY = fromRowIndex * rowHeight + rowHeight / 2;
    const toMidY = toRowIndex * rowHeight + rowHeight / 2;

    let x1 = 0, x2 = 0;

    switch (type) {
      case 'FS':
        x1 = from.plannedEnd ? this.dateToX(from.plannedEnd, timelineStart, pixelsPerDay) : 0;
        x2 = to.plannedStart ? this.dateToX(to.plannedStart, timelineStart, pixelsPerDay) : 0;
        break;
      case 'SS':
        x1 = from.plannedStart ? this.dateToX(from.plannedStart, timelineStart, pixelsPerDay) : 0;
        x2 = to.plannedStart ? this.dateToX(to.plannedStart, timelineStart, pixelsPerDay) : 0;
        break;
      case 'FF':
        x1 = from.plannedEnd ? this.dateToX(from.plannedEnd, timelineStart, pixelsPerDay) : 0;
        x2 = to.plannedEnd ? this.dateToX(to.plannedEnd, timelineStart, pixelsPerDay) : 0;
        break;
      case 'SF':
        x1 = from.plannedStart ? this.dateToX(from.plannedStart, timelineStart, pixelsPerDay) : 0;
        x2 = to.plannedEnd ? this.dateToX(to.plannedEnd, timelineStart, pixelsPerDay) : 0;
        break;
    }

    const midX = (x1 + x2) / 2;
    return `M ${x1} ${fromMidY} L ${midX} ${fromMidY} L ${midX} ${toMidY} L ${x2} ${toMidY}`;
  }
}
