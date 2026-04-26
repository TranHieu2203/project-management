import { ChangeDetectionStrategy, Component, Input, computed, signal } from '@angular/core';
import { DatePipe, NgIf } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ResourceOverloadResult } from '../../models/overload.model';

@Component({
  selector: 'app-overload-warning-banner',
  standalone: true,
  imports: [NgIf, DatePipe, MatIconModule, MatProgressSpinnerModule, MatTooltipModule],
  templateUrl: './overload-warning-banner.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OverloadWarningBannerComponent {
  @Input() result: ResourceOverloadResult | null = null;
  @Input() loading = false;
  @Input() lastUpdated: string | null = null;

  get overloadedDays(): number {
    return this.result?.dailyBreakdown.filter(d => d.isOverloaded).length ?? 0;
  }

  get overloadedWeeks(): number {
    return this.result?.weeklyBreakdown.filter(w => w.isOverloaded).length ?? 0;
  }

  get tooltipText(): string {
    if (!this.result) return '';
    const days = this.result.dailyBreakdown
      .filter(d => d.isOverloaded)
      .map(d => `${d.date}: ${d.hours.toFixed(1)}h (OL-01)`)
      .join('\n');
    const weeks = this.result.weeklyBreakdown
      .filter(w => w.isOverloaded)
      .map(w => `Tuần ${w.weekStart}: ${w.totalHours.toFixed(1)}h (OL-02)`)
      .join('\n');
    return [days, weeks].filter(Boolean).join('\n');
  }
}
