import { ChangeDetectionStrategy, Component, inject, OnDestroy, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { DashboardFacade } from '../../store/dashboard.facade';

@Component({
  standalone: true,
  selector: 'app-dashboard-shell',
  imports: [RouterOutlet],
  template: `<router-outlet />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardShellComponent implements OnInit, OnDestroy {
  private readonly facade = inject(DashboardFacade);

  ngOnInit(): void {
    this.facade.startPolling();
  }

  ngOnDestroy(): void {
    this.facade.stopPolling();
  }
}
