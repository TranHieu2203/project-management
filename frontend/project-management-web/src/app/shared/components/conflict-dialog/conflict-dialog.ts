import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

export interface ConflictDialogData {
  serverState: unknown;
  userChanges: unknown;
  eTag: string;
}

export type ConflictDialogResult = 'use-server' | 'retry-mine';

@Component({
  selector: 'app-conflict-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Xung đột dữ liệu</h2>
    <mat-dialog-content>
      <p>Dữ liệu đã được cập nhật bởi người khác. Bạn muốn làm gì?</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="close('use-server')">Dùng bản mới nhất</button>
      <button mat-flat-button color="primary" (click)="close('retry-mine')">Thử áp lại của tôi</button>
    </mat-dialog-actions>
  `,
})
export class ConflictDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<ConflictDialogComponent>);
  readonly data = inject<ConflictDialogData>(MAT_DIALOG_DATA);

  close(result: ConflictDialogResult): void {
    this.dialogRef.close(result);
  }
}
