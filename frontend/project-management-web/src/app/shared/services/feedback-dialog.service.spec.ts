import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { FeedbackDialogService } from './feedback-dialog.service';

describe('FeedbackDialogService', () => {
  let svc: FeedbackDialogService;
  const mockOpen = vi.fn();

  beforeEach(() => {
    mockOpen.mockReset();
    vi.spyOn(console, 'error').mockImplementation(() => {});

    TestBed.configureTestingModule({
      providers: [
        FeedbackDialogService,
        { provide: MatDialog, useValue: { open: mockOpen } },
      ],
    });
    svc = TestBed.inject(FeedbackDialogService);
  });

  describe('extractTraceId', () => {
    it('returns traceId from HttpErrorResponse.error.traceId (ProblemDetails)', () => {
      const err = { error: { traceId: 'trace-abc-123' } };
      expect(svc.extractTraceId(err)).toBe('trace-abc-123');
    });

    it('returns traceId from top-level err.traceId', () => {
      const err = { traceId: 'direct-trace' };
      expect(svc.extractTraceId(err)).toBe('direct-trace');
    });

    it('returns undefined for plain Error object', () => {
      expect(svc.extractTraceId(new Error('oops'))).toBeUndefined();
    });

    it('returns undefined for null', () => {
      expect(svc.extractTraceId(null)).toBeUndefined();
    });

    it('returns undefined for non-object', () => {
      expect(svc.extractTraceId('string-error')).toBeUndefined();
    });
  });

  describe('success()', () => {
    it('calls MatDialog.open with mode=success and disableClose=false', () => {
      svc.success('Lưu thành công');

      expect(mockOpen).toHaveBeenCalledOnce();
      const [, config] = mockOpen.mock.calls[0];
      expect(config.data.mode).toBe('success');
      expect(config.data.message).toBe('Lưu thành công');
      expect(config.disableClose).toBe(false);
      expect(config.width).toBe('380px');
    });
  });

  describe('error()', () => {
    it('calls MatDialog.open with mode=error and disableClose=true', () => {
      svc.error('Lỗi tạo task');

      expect(mockOpen).toHaveBeenCalledOnce();
      const [, config] = mockOpen.mock.calls[0];
      expect(config.data.mode).toBe('error');
      expect(config.data.message).toBe('Lỗi tạo task');
      expect(config.disableClose).toBe(true);
      expect(config.width).toBe('420px');
    });

    it('extracts traceId and passes it to dialog data', () => {
      const err = { error: { traceId: 'tr-999' } };
      svc.error('API lỗi', err);

      const [, config] = mockOpen.mock.calls[0];
      expect(config.data.traceId).toBe('tr-999');
    });

    it('calls console.error with structured format', () => {
      svc.error('Test error', { error: { traceId: 'tr-001' } });

      expect(console.error).toHaveBeenCalledWith(
        '[Error][traceId: tr-001]',
        'Test error',
        expect.objectContaining({ traceId: 'tr-001', message: 'Test error' }),
      );
    });

    it('uses n/a when no traceId available', () => {
      svc.error('Lỗi không có trace');

      expect(console.error).toHaveBeenCalledWith(
        '[Error][traceId: n/a]',
        'Lỗi không có trace',
        expect.any(Object),
      );
    });
  });
});
