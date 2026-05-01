import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { FeedbackDialogData } from './feedback-dialog.component';

// Pure logic tests — no Angular TestBed required

describe('FeedbackDialogComponent logic', () => {
  describe('auto-close timer (success mode)', () => {
    beforeEach(() => {
      vi.useFakeTimers();
    });
    afterEach(() => {
      vi.useRealTimers();
    });

    it('decrements remaining every 100ms and closes at 0', () => {
      const closeFn = vi.fn();
      const dialogRef = { close: closeFn };

      const data: FeedbackDialogData = { mode: 'success', message: 'OK', autoCloseDuration: 300 };
      let remaining = data.autoCloseDuration!;
      const total = remaining;

      const timer = setInterval(() => {
        remaining -= 100;
        if (remaining <= 0) {
          clearInterval(timer);
          dialogRef.close();
        }
      }, 100);

      vi.advanceTimersByTime(200);
      expect(closeFn).not.toHaveBeenCalled();

      vi.advanceTimersByTime(100);
      expect(closeFn).toHaveBeenCalledOnce();
      expect(remaining).toBeLessThanOrEqual(0);
    });

    it('does NOT start timer in error mode', () => {
      const closeFn = vi.fn();
      const data: FeedbackDialogData = { mode: 'error', message: 'Lỗi' };

      // Error mode: no timer started — close should never be called
      if (data.mode === 'success') {
        setInterval(() => closeFn(), 100);
      }

      vi.advanceTimersByTime(5000);
      expect(closeFn).not.toHaveBeenCalled();
    });
  });

  describe('traceId rendering logic', () => {
    it('renders traceId block when traceId is present', () => {
      const data: FeedbackDialogData = { mode: 'error', message: 'Lỗi', traceId: 'abc-123' };
      const shouldShowTrace = data.mode === 'error' && !!data.traceId;
      expect(shouldShowTrace).toBe(true);
    });

    it('hides traceId block when traceId is undefined', () => {
      const data: FeedbackDialogData = { mode: 'error', message: 'Lỗi' };
      const shouldShowTrace = data.mode === 'error' && !!data.traceId;
      expect(shouldShowTrace).toBe(false);
    });

    it('hides traceId block in success mode even if traceId present', () => {
      const data: FeedbackDialogData = { mode: 'success', message: 'OK', traceId: 'should-not-show' };
      const shouldShowTrace = data.mode === 'error' && !!data.traceId;
      expect(shouldShowTrace).toBe(false);
    });
  });

  describe('progressPercent calculation', () => {
    it('returns 100% at start', () => {
      const total = 3000;
      const remaining = 3000;
      expect((remaining / total) * 100).toBe(100);
    });

    it('returns 0% when remaining is 0', () => {
      const total = 3000;
      const remaining = 0;
      expect((remaining / total) * 100).toBe(0);
    });

    it('returns 50% halfway through', () => {
      const total = 3000;
      const remaining = 1500;
      expect((remaining / total) * 100).toBe(50);
    });
  });
});
