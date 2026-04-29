import {
  ChangeDetectionStrategy, Component, computed, inject, OnInit, signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DeadlineAlertService } from '../../services/deadline-alert.service';
import { MyTask, MyTasksApiService } from '../../services/my-tasks-api.service';

interface TaskGroup {
  label: string;
  icon: string;
  colorClass: string;
  tasks: MyTask[];
}

@Component({
  standalone: true,
  selector: 'app-my-tasks',
  imports: [NgClass, FormsModule, MatIconModule, MatProgressSpinnerModule, MatTooltipModule],
  templateUrl: './my-tasks.html',
  styleUrl: './my-tasks.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyTasksComponent implements OnInit {
  private readonly api = inject(MyTasksApiService);
  private readonly router = inject(Router);
  private readonly deadlineService = inject(DeadlineAlertService);

  readonly today = this.deadlineService.getLocalDateString();

  loading = signal(true);
  error = signal<string | null>(null);
  allTasks = signal<MyTask[]>([]);
  keyword = signal('');

  readonly groups = computed<TaskGroup[]>(() => {
    const kw = this.keyword().toLowerCase();
    const tasks = kw
      ? this.allTasks().filter(t =>
          t.name.toLowerCase().includes(kw) ||
          (t.vbs?.toLowerCase().includes(kw) ?? false) ||
          t.projectName.toLowerCase().includes(kw)
        )
      : this.allTasks();

    const overdue: MyTask[] = [];
    const dueToday: MyTask[] = [];
    const dueSoon: MyTask[] = [];
    const rest: MyTask[] = [];
    const done: MyTask[] = [];

    for (const t of tasks) {
      if (t.status === 'Completed') {
        done.push(t);
        continue;
      }
      const s = this.deadlineService.getDeadlineStatusRaw(
        t.plannedEndDate, t.type, t.status, this.today
      );
      if (s === 'overdue')       overdue.push(t);
      else if (s === 'due-today') dueToday.push(t);
      else if (s === 'due-soon')  dueSoon.push(t);
      else                        rest.push(t);
    }

    const result: TaskGroup[] = [];
    if (overdue.length)   result.push({ label: 'Quá hạn',        icon: 'error',         colorClass: 'group-overdue',   tasks: overdue });
    if (dueToday.length)  result.push({ label: 'Hôm nay',        icon: 'today',         colorClass: 'group-due-today', tasks: dueToday });
    if (dueSoon.length)   result.push({ label: 'Sắp đến hạn',    icon: 'schedule',      colorClass: 'group-due-soon',  tasks: dueSoon });
    if (rest.length)      result.push({ label: 'Các task khác',  icon: 'inbox',         colorClass: 'group-rest',      tasks: rest });
    if (done.length)      result.push({ label: 'Đã hoàn thành',  icon: 'check_circle',  colorClass: 'group-done',      tasks: done });
    return result;
  });

  ngOnInit(): void {
    this.api.getMyTasks().subscribe({
      next: tasks => {
        this.allTasks.set(tasks);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Không thể tải danh sách tasks.');
        this.loading.set(false);
      },
    });
  }

  navigateToTask(task: MyTask): void {
    this.router.navigate(['/projects', task.projectId], {
      queryParams: { highlight: task.id },
    });
  }

  priorityLabel(p: string): string {
    return p;
  }

  statusLabel(s: string): string {
    const map: Record<string, string> = {
      NotStarted: 'Chưa bắt đầu',
      InProgress: 'Đang làm',
      OnHold: 'Tạm dừng',
      Delayed: 'Bị trễ',
      Completed: 'Hoàn thành',
    };
    return map[s] ?? s;
  }

  formatDate(d: string | null): string {
    if (!d) return '—';
    const [y, m, day] = d.split('-');
    return `${day}/${m}/${y}`;
  }
}
