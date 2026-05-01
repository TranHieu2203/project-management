import { Component, inject, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';

export type FeedbackDialogMode = 'success' | 'error';

export interface FeedbackDialogData {
  mode: FeedbackDialogMode;
  message: string;
  traceId?: string;
  autoCloseDuration?: number;
}

@Component({
  selector: 'app-feedback-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <div class="feedback-dialog-container">
      <div class="feedback-header">
        <mat-icon [style.color]="data.mode === 'success' ? '#4CAF50' : '#F44336'">
          {{ data.mode === 'success' ? 'check_circle' : 'error' }}
        </mat-icon>
        <h2 mat-dialog-title>{{ data.mode === 'success' ? 'Thành công' : 'Đã xảy ra lỗi' }}</h2>
      </div>

      <mat-dialog-content>
        <p class="feedback-message">{{ data.message }}</p>
        @if (data.mode === 'error' && data.traceId) {
          <p class="trace-id">Mã lỗi: <code>{{ data.traceId }}</code></p>
        }
      </mat-dialog-content>

      @if (data.mode === 'success') {
        <div class="progress-bar-container">
          <div class="progress-bar" [style.width.%]="progressPercent"></div>
        </div>
      }

      @if (data.mode === 'error') {
        <mat-dialog-actions align="end">
          <button mat-flat-button color="primary" (click)="dialogRef.close()">Xác nhận</button>
        </mat-dialog-actions>
      }
    </div>
  `,
  styles: [`
    .feedback-dialog-container {
      background: #ffffff;
      color: #111111;
      min-width: 320px;
    }

    .feedback-header {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 16px 24px 0;
    }

    .feedback-header mat-icon {
      font-size: 28px;
      width: 28px;
      height: 28px;
    }

    h2[mat-dialog-title] {
      margin: 0;
      font-size: 18px;
      font-weight: 500;
      color: #111111;
      padding: 0;
    }

    .feedback-message {
      margin: 0;
      color: #333333;
      font-size: 14px;
      line-height: 1.5;
    }

    .trace-id {
      margin: 8px 0 0;
      font-size: 12px;
      color: #555555;
    }

    .trace-id code {
      font-family: monospace;
      background: #f5f5f5;
      padding: 2px 6px;
      border-radius: 3px;
      user-select: all;
      color: #333333;
    }

    .progress-bar-container {
      height: 3px;
      background: #e0e0e0;
      margin-top: 8px;
    }

    .progress-bar {
      height: 100%;
      background: #4CAF50;
      transition: width 100ms linear;
    }
  `],
})
export class FeedbackDialogComponent implements OnInit, OnDestroy {
  readonly dialogRef = inject(MatDialogRef<FeedbackDialogComponent>);
  readonly data = inject<FeedbackDialogData>(MAT_DIALOG_DATA);
  private readonly cdr = inject(ChangeDetectorRef);

  private timer: ReturnType<typeof setInterval> | undefined;
  private readonly total: number = this.data.autoCloseDuration ?? 3000;
  remaining = this.total;

  get progressPercent(): number {
    return (this.remaining / this.total) * 100;
  }

  ngOnInit(): void {
    if (this.data.mode === 'success') {
      this.timer = setInterval(() => {
        this.remaining -= 100;
        this.cdr.markForCheck();
        if (this.remaining <= 0) {
          clearInterval(this.timer);
          this.dialogRef.close();
        }
      }, 100);
    }
  }

  ngOnDestroy(): void {
    clearInterval(this.timer);
  }
}
