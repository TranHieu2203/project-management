import { Injectable } from '@angular/core';
import { ProjectTask } from '../models/task.model';

export type DeadlineStatus = 'overdue' | 'due-today' | 'due-soon' | 'none';

export interface DeadlineSummary {
  overdue: ProjectTask[];
  dueToday: ProjectTask[];
  dueSoon: ProjectTask[];
}

const EXCLUDED_TYPES: ReadonlyArray<string> = ['Phase', 'Milestone'];
const DONE_STATUSES = new Set(['Completed', 'Cancelled']);
const DUE_SOON_DAYS = 7;

@Injectable({ providedIn: 'root' })
export class DeadlineAlertService {

  getLocalDateString(): string {
    const d = new Date();
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  getDeadlineStatus(task: ProjectTask, today: string): DeadlineStatus {
    return this.getDeadlineStatusRaw(task.plannedEndDate, task.type, task.status, today);
  }

  getDeadlineStatusRaw(
    plannedEndDate: string | null,
    type: string,
    status: string,
    today: string,
  ): DeadlineStatus {
    if (EXCLUDED_TYPES.includes(type)) return 'none';
    if (!plannedEndDate || DONE_STATUSES.has(status)) return 'none';
    if (plannedEndDate < today) return 'overdue';
    if (plannedEndDate === today) return 'due-today';
    if (plannedEndDate <= this.addDays(today, DUE_SOON_DAYS)) return 'due-soon';
    return 'none';
  }

  computeDeadlineSummary(tasks: ProjectTask[], today: string): DeadlineSummary {
    const summary: DeadlineSummary = { overdue: [], dueToday: [], dueSoon: [] };
    for (const task of tasks) {
      const s = this.getDeadlineStatus(task, today);
      if (s === 'overdue') summary.overdue.push(task);
      else if (s === 'due-today') summary.dueToday.push(task);
      else if (s === 'due-soon') summary.dueSoon.push(task);
    }
    return summary;
  }

  dateToLocalString(date: Date): string {
    const yyyy = date.getFullYear();
    const mm = String(date.getMonth() + 1).padStart(2, '0');
    const dd = String(date.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  private addDays(date: string, days: number): string {
    const d = new Date(date + 'T00:00:00');
    d.setDate(d.getDate() + days);
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }
}
