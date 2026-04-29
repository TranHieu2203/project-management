import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { toObservable } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LowerCasePipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { combineLatest, of, switchMap } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { DashboardFacade } from '../../store/dashboard.facade';
import { DashboardApiService, DashboardMyTaskDto } from '../../services/dashboard-api.service';

const PAGE_SIZE = 20;

const STATUS_OPTIONS: { value: string | null; label: string }[] = [
  { value: null, label: 'Tất cả' },
  { value: 'NotStarted', label: 'Chưa bắt đầu' },
  { value: 'InProgress', label: 'Đang thực hiện' },
  { value: 'OnHold', label: 'Tạm dừng' },
  { value: 'Delayed', label: 'Bị trễ' },
  { value: 'Completed', label: 'Hoàn thành' },
];

@Component({
  standalone: true,
  selector: 'app-dashboard-my-tasks',
  imports: [
    NgClass,
    LowerCasePipe,
    FormsModule,
    RouterLink,
    MatSelectModule,
    MatFormFieldModule,
    MatButtonModule,
    MatIconModule,
  ],
  templateUrl: './my-tasks.html',
  styleUrl: './my-tasks.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardMyTasksComponent implements OnInit {
  private readonly facade = inject(DashboardFacade);
  private readonly api = inject(DashboardApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly STATUS_OPTIONS = STATUS_OPTIONS;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly tasks = signal<DashboardMyTaskDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly statusFilter = signal<string | null>(null);

  readonly totalPages = computed(() => Math.ceil(this.totalCount() / PAGE_SIZE));
  readonly showPagination = computed(() => this.totalCount() > PAGE_SIZE);
  readonly hasPrevPage = computed(() => this.page() > 1);
  readonly hasNextPage = computed(() => this.page() < this.totalPages());
  readonly hasActiveFilter = computed(() => this.statusFilter() !== null);

  ngOnInit(): void {
    const params = this.route.snapshot.queryParams;
    this.page.set(Number(params['page'] ?? 1));
    this.statusFilter.set(params['status'] ?? null);

    combineLatest([
      this.facade.selectedProjectIds$,
      toObservable(this.page),
      toObservable(this.statusFilter),
    ]).pipe(
      switchMap(([projectIds, page, status]) => {
        this.loading.set(true);
        this.error.set(null);
        return this.api.getMyTasks({
          page,
          pageSize: PAGE_SIZE,
          status,
          projectIds,
        }).pipe(
          catchError(() => {
            this.error.set('Không thể tải danh sách tasks. Thử lại?');
            this.loading.set(false);
            return of(null);
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(result => {
      if (result) {
        this.tasks.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      }
    });
  }

  onStatusChange(status: string | null): void {
    this.statusFilter.set(status);
    this.page.set(1);
    this.syncUrl();
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.syncUrl();
  }

  retry(): void {
    const p = this.page();
    this.page.set(0);
    this.page.set(p);
  }

  navigateToTask(task: DashboardMyTaskDto): void {
    this.router.navigate(['/projects', task.projectId], {
      queryParams: { view: 'grid', highlight: task.id },
    });
  }

  formatDate(d: string | null): string {
    if (!d) return '—';
    const [y, m, day] = d.split('-');
    return `${day}/${m}/${y}`;
  }

  statusLabel(s: string): string {
    return STATUS_OPTIONS.find(o => o.value === s)?.label ?? s;
  }

  private syncUrl(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParamsHandling: 'merge',
      queryParams: {
        status: this.statusFilter() ?? null,
        page: this.page() > 1 ? this.page() : null,
      },
    });
  }
}
