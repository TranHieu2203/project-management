import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { StatCards } from '../../../models/dashboard.model';

@Component({
  standalone: true,
  selector: 'app-stat-cards',
  imports: [MatIconModule, MatTooltipModule],
  templateUrl: './stat-cards.html',
  styleUrl: './stat-cards.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatCardsComponent {
  @Input({ required: true }) statCards: StatCards | null = null;
  @Input() loading = false;
  @Input() error: string | null = null;
  @Output() overdueClick = new EventEmitter<void>();
  @Output() atRiskClick = new EventEmitter<void>();
  @Output() overloadedClick = new EventEmitter<void>();
}
