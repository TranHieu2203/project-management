import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { DecimalPipe, NgStyle } from '@angular/common';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  standalone: true,
  selector: 'app-project-pulse-strip',
  imports: [NgStyle, DecimalPipe, MatTooltipModule],
  templateUrl: './project-pulse-strip.html',
  styleUrl: './project-pulse-strip.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectPulseStripComponent {
  @Input({ required: true }) percentComplete: number = 0;
  @Input({ required: true }) percentTimeElapsed: number = 0;
  @Input({ required: true }) remainingTaskCount: number = 0;

  get circumference(): number { return 2 * Math.PI * 20; }

  get dashOffset(): number {
    const pct = Math.max(0, Math.min(100, this.percentComplete));
    return this.circumference * (1 - pct / 100);
  }
}
