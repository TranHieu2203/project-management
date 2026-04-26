import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';

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
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatToolbarModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
  ],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShellComponent {
  readonly sidenavExpanded = signal<boolean>(
    localStorage.getItem(SIDENAV_KEY) !== 'false'
  );

  readonly navItems: NavItem[] = [
    { label: 'Dự án', icon: 'folder_open', route: '/projects' },
    { label: 'Vendor', icon: 'business', route: '/vendors' },
    { label: 'Nhân sự', icon: 'people', route: '/resources' },
    { label: 'Đơn giá', icon: 'attach_money', route: '/rates' },
    { label: 'Audit', icon: 'history', route: '/audit' },
    { label: 'Chấm công', icon: 'access_time', route: '/time-tracking' },
    { label: 'Capacity', icon: 'bar_chart', route: '/capacity' },
    { label: 'Báo cáo', icon: 'assessment', route: '/reporting' },
    { label: 'Thông báo', icon: 'notifications', route: '/settings/notifications' },
  ];

  toggleSidenav(): void {
    const next = !this.sidenavExpanded();
    this.sidenavExpanded.set(next);
    localStorage.setItem(SIDENAV_KEY, next.toString());
  }
}
