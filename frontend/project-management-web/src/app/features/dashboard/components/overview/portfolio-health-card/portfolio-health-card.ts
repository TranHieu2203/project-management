import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { ProjectSummary } from '../../../models/dashboard.model';
import { ProjectPulseStripComponent } from '../project-pulse-strip/project-pulse-strip';

@Component({
  standalone: true,
  selector: 'app-portfolio-health-card',
  imports: [NgClass, RouterLink, MatIconModule, ProjectPulseStripComponent],
  templateUrl: './portfolio-health-card.html',
  styleUrl: './portfolio-health-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PortfolioHealthCardComponent {
  @Input({ required: true }) project!: ProjectSummary;

  get statusConfig(): { label: string; icon: string; cssClass: string } {
    switch (this.project.healthStatus) {
      case 'OnTrack':
        return { label: 'Đúng tiến độ', icon: 'check_circle', cssClass: 'status-on-track' };
      case 'AtRisk':
        return { label: 'Có nguy cơ', icon: 'warning', cssClass: 'status-at-risk' };
      case 'Delayed':
        return { label: 'Bị trễ', icon: 'error', cssClass: 'status-delayed' };
    }
  }
}
