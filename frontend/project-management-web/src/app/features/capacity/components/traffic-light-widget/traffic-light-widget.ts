import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { DecimalPipe, NgFor, NgIf } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { CapacityUtilizationResult } from '../../models/utilization.model';

@Component({
  selector: 'app-traffic-light-widget',
  standalone: true,
  imports: [NgIf, NgFor, DecimalPipe, MatButtonModule],
  templateUrl: './traffic-light-widget.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TrafficLightWidgetComponent {
  @Input() utilization: CapacityUtilizationResult | null = null;
  @Input() loading = false;
  @Output() override = new EventEmitter<void>();

  get statusColor(): string {
    const colors: Record<string, string> = {
      Green: '#2e7d32', Yellow: '#f9a825', Orange: '#e65100', Red: '#c62828',
    };
    return colors[this.utilization?.trafficLight ?? 'Green'] ?? '#2e7d32';
  }

  get statusLabel(): string {
    const labels: Record<string, string> = {
      Green: 'Bình thường', Yellow: 'Chú ý', Orange: 'Cảnh báo', Red: 'Quá tải',
    };
    return labels[this.utilization?.trafficLight ?? 'Green'] ?? '';
  }

  get canOverride(): boolean {
    return !!this.utilization && this.utilization.trafficLight !== 'Green';
  }
}
