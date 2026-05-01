import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  inject,
} from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { SettingsApiService } from '../services/settings-api.service';

@Component({
  selector: 'app-notification-preferences',
  standalone: true,
  imports: [NgFor, NgIf, MatCardModule, MatProgressSpinnerModule, MatSlideToggleModule],
  templateUrl: './notification-preferences.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationPreferencesComponent implements OnInit {
  private readonly api = inject(SettingsApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  preferences: { type: string; label: string; isEnabled: boolean }[] = [];
  loading = true;

  ngOnInit(): void {
    this.api.getNotificationPreferences().subscribe(prefs => {
      this.preferences = prefs.map(p => ({
        type: p.type,
        label: this.getLabel(p.type),
        isEnabled: p.isEnabled,
      }));
      this.loading = false;
      this.cdr.markForCheck();
    });
  }

  private getLabel(type: string): string {
    const labels: Record<string, string> = {
      'overload':        'Cảnh báo Overload',
      'overdue':         'Task sắp trễ',
      'assigned':        'Được giao task',
      'commented':       'Có comment mới',
      'status-changed':  'Task thay đổi trạng thái',
      'mentioned':       '@mention trong comment',
    };
    return labels[type] ?? type;
  }

  toggle(type: string, isEnabled: boolean): void {
    this.api.updateNotificationPreference(type, isEnabled).subscribe();
    const pref = this.preferences.find(p => p.type === type);
    if (pref) { pref.isEnabled = isEnabled; this.cdr.markForCheck(); }
  }
}
