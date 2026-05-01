import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { FeedbackDialogComponent, FeedbackDialogData } from '../components/feedback-dialog/feedback-dialog.component';

@Injectable({ providedIn: 'root' })
export class FeedbackDialogService {
  private readonly dialog = inject(MatDialog);

  success(message: string): void {
    this.dialog.open(FeedbackDialogComponent, {
      data: { mode: 'success', message } satisfies FeedbackDialogData,
      width: '380px',
      disableClose: false,
      panelClass: 'feedback-dialog-panel',
    });
  }

  error(message: string, err?: unknown): void {
    const traceId = this.extractTraceId(err);
    const logPayload = { traceId, message, err };
    console.error(`[Error][traceId: ${traceId ?? 'n/a'}]`, message, logPayload);

    this.dialog.open(FeedbackDialogComponent, {
      data: { mode: 'error', message, traceId } satisfies FeedbackDialogData,
      width: '420px',
      disableClose: true,
      panelClass: 'feedback-dialog-panel',
    });
  }

  extractTraceId(err: unknown): string | undefined {
    if (!err || typeof err !== 'object') return undefined;
    const e = err as Record<string, unknown>;
    if (e['error'] && typeof e['error'] === 'object') {
      return (e['error'] as Record<string, unknown>)['traceId'] as string | undefined;
    }
    return e['traceId'] as string | undefined;
  }
}
