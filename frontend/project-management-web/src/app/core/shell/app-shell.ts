import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { AsyncPipe, NgIf } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Store } from '@ngrx/store';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatBadgeModule } from '@angular/material/badge';
import { AlertActions } from '../../features/alerts/store/alert.actions';
import { selectUnreadCount, selectPanelOpen } from '../../features/alerts/store/alert.reducer';
import { AlertPanelComponent } from '../../features/alerts/components/alert-panel/alert-panel';

const SIDENAV_KEY = 'pm_sidenav_expanded';

interface NavItem {
  label: string;
  icon: string;
  route: string;
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    AsyncPipe, NgIf,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatToolbarModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatBadgeModule,
    AlertPanelComponent,
  ],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShellComponent implements OnInit {
  private readonly store = inject(Store);

  readonly sidenavExpanded = signal<boolean>(
    localStorage.getItem(SIDENAV_KEY) !== 'false'
  );

  readonly unreadCount$ = this.store.select(selectUnreadCount);
  readonly panelOpen$ = this.store.select(selectPanelOpen);

  readonly navItems: NavItem[] = [
    { label: 'Dashboard', icon: 'dashboard', route: '/dashboard' },
    { label: 'My Tasks', icon: 'task_alt', route: '/my-tasks' },
    { label: 'Dự án', icon: 'folder_open', route: '/projects' },
    { label: 'Vendor', icon: 'business', route: '/vendors' },
    { label: 'Nhân sự', icon: 'people', route: '/resources' },
    { label: 'Đơn giá', icon: 'attach_money', route: '/rates' },
    { label: 'Audit', icon: 'history', route: '/audit' },
    { label: 'Chấm công', icon: 'access_time', route: '/time-tracking' },
    { label: 'Capacity', icon: 'bar_chart', route: '/capacity' },
    { label: 'Báo cáo', icon: 'assessment', route: '/reporting' },
    { label: 'Budget Report', icon: 'account_balance_wallet', route: '/reports/budget' },
    { label: 'Thông báo', icon: 'notifications', route: '/settings/notifications' },
  ];

  ngOnInit(): void {
    this.store.dispatch(AlertActions.loadAlerts());
  }

  toggleSidenav(): void {
    const next = !this.sidenavExpanded();
    this.sidenavExpanded.set(next);
    localStorage.setItem(SIDENAV_KEY, next.toString());
  }

  toggleAlertPanel(): void {
    this.store.dispatch(AlertActions.togglePanel());
  }

  closeAlertPanel(): void {
    this.store.dispatch(AlertActions.closePanel());
  }
}
