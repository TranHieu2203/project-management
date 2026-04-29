import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { combineLatest, map } from 'rxjs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { DashboardFacade } from '../../store/dashboard.facade';
import { DashboardActions } from '../../store/dashboard.actions';
import { DEFAULT_FILTERS, DashboardFilters, Deadline } from '../../models/dashboard.model';
import {
  selectDashboardFilters,
  selectHasActiveFilters,
  selectProjects,
  selectQuickChips,
} from '../../store/dashboard.selectors';
import { PortfolioHealthCardComponent } from './portfolio-health-card/portfolio-health-card';
import { StatCardsComponent } from './stat-cards/stat-cards';
import { UpcomingDeadlinesComponent } from './upcoming-deadlines/upcoming-deadlines';
import { DashboardFilterBarComponent } from '../filter-bar/dashboard-filter-bar';

@Component({
  standalone: true,
  selector: 'app-dashboard-overview',
  imports: [
    AsyncPipe,
    MatProgressSpinnerModule,
    MatIconModule,
    PortfolioHealthCardComponent,
    StatCardsComponent,
    UpcomingDeadlinesComponent,
    DashboardFilterBarComponent,
  ],
  templateUrl: './dashboard-overview.html',
  styleUrl: './dashboard-overview.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardOverviewComponent {
  private readonly facade = inject(DashboardFacade);
  private readonly store = inject(Store);
  private readonly router = inject(Router);

  readonly defaultFilters = DEFAULT_FILTERS;

  readonly projects$ = this.facade.projects$;
  readonly loadingProjects$ = this.facade.loadingProjects$;
  readonly errorProjects$ = this.facade.errorProjects$;
  readonly lastUpdatedAt$ = this.facade.lastUpdatedAt$;

  readonly statCards$ = this.facade.statCards$;
  readonly loadingStatCards$ = this.facade.loadingStatCards$;
  readonly errorStatCards$ = this.facade.errorStatCards$;

  readonly deadlines$ = this.facade.deadlines$;
  readonly loadingDeadlines$ = this.facade.loadingDeadlines$;
  readonly errorDeadlines$ = this.facade.errorDeadlines$;

  readonly filters$ = this.store.select(selectDashboardFilters);
  readonly hasActiveFilters$ = this.store.select(selectHasActiveFilters);

  readonly filteredProjects$ = combineLatest([
    this.store.select(selectProjects),
    this.store.select(selectQuickChips),
  ]).pipe(
    map(([projects, chips]) => {
      if (!chips.length) return projects;
      return projects.filter(p => {
        if (chips.includes('overdue') && p.overdueTaskCount === 0) return false;
        if (chips.includes('atRisk') && !['AtRisk', 'Delayed'].includes(p.healthStatus)) return false;
        return true;
      });
    })
  );

  formatTime(ts: number | null): string {
    if (!ts) return '';
    const d = new Date(ts);
    return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
  }

  onFiltersChange(filters: DashboardFilters): void {
    this.store.dispatch(DashboardActions.setFilters({ filters }));
  }

  onDeadlineClick(deadline: Deadline): void {
    this.router.navigate(['/projects', deadline.projectId], {
      queryParams: { view: 'gantt', highlight: deadline.taskId },
    });
  }

  onOverdueCardClick(): void {
    this.router.navigate(['/dashboard/my-tasks'], {
      queryParams: { status: 'overdue' },
    });
  }

  onAtRiskCardClick(): void {
    this.router.navigate(['/dashboard/overview'], {
      queryParams: { chips: 'atRisk' },
    });
  }

  onOverloadedCardClick(): void {
    this.router.navigate(['/dashboard/overview'], {
      queryParams: { chips: 'overloaded' },
    });
  }
}
