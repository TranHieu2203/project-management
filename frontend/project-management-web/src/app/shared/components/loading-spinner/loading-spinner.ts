import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  template: `
    @if (visible) {
      <div class="spinner-overlay">
        <mat-spinner [diameter]="diameter" />
      </div>
    }
  `,
  styles: [`
    .spinner-overlay {
      display: flex;
      justify-content: center;
      align-items: center;
      padding: 16px;
    }
  `],
})
export class LoadingSpinnerComponent {
  @Input() visible = true;
  @Input() diameter = 40;
}
