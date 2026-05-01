# Cross-Cutting Story CC-01: FeedbackDialogService — Thay thế MatSnackBar bằng Dialog chuẩn hóa

Status: review

**Story ID:** CC-01
**Epic:** Cross-Cutting Infrastructure (bắt buộc trước mọi story Phase 2+)
**Sprint:** Hiện tại (chèn trước Story 7-5)
**Date Created:** 2026-04-29
**Priority:** BLOCKER — Mọi story sau phải dùng FeedbackDialogService, không dùng MatSnackBar

---

## Story

As a developer,
I want có một `FeedbackDialogService` chuẩn hóa để thay thế toàn bộ `MatSnackBar`,
So that người dùng nhận được feedback nhất quán (success auto-close 3s / error có traceId + nút xác nhận), và dev agent không được tự ý gọi MatSnackBar trong bất kỳ story nào sau CC-01.

---

## Acceptance Criteria

1. **Given** bất kỳ thao tác nào thành công
   **When** component gọi `feedbackDialog.success('Lưu thành công')`
   **Then** hiển thị dialog overlay có checkmark xanh, message text, tự động đóng sau **3 giây**
   **And** có progress bar đếm ngược trực quan (animation 3s)
   **And** user có thể click ngoài để đóng sớm

2. **Given** bất kỳ thao tác nào gặp lỗi (API error, validation fail, v.v.)
   **When** component gọi `feedbackDialog.error('Lỗi tạo task', error)`
   **Then** hiển thị dialog overlay có icon đỏ, message text, **traceId** nếu có trong error response
   **And** dialog KHÔNG tự đóng — phải có nút **"Xác nhận"** để user dismiss
   **And** `console.error('[FeedbackDialog]', traceId, message, error)` được gọi với structured data

3. **Given** API trả về `ProblemDetails` với field `traceId`
   **When** error dialog hiển thị
   **Then** traceId được render dưới dạng `Mã lỗi: <traceId>` trong dialog body (monospace font, có thể copy)

4. **Given** có lỗi xảy ra trong component
   **When** error được log qua FeedbackDialogService
   **Then** console output format: `[Error][traceId: xxx] message` (có timestamp, có thể filter trong DevTools)

5. **Given** toàn bộ `MatSnackBar.open(...)` hiện tại (3 files)
   **When** migration hoàn thành
   **Then** không còn `MatSnackBar` import nào trong codebase (trừ `FeedbackDialogService` nội bộ nếu cần)

6. **Given** `FeedbackDialogService` được đăng ký `providedIn: 'root'`
   **When** bất kỳ component nào inject `FeedbackDialogService`
   **Then** không cần import MatSnackBarModule, không cần thêm providers

---

## Tasks / Subtasks

- [x] **Task 1: Tạo FeedbackDialogComponent** (AC: 1, 2, 3)
  - [x] 1.1 Tạo `frontend/.../shared/components/feedback-dialog/feedback-dialog.component.ts`:
    ```typescript
    export type FeedbackDialogMode = 'success' | 'error';

    export interface FeedbackDialogData {
      mode: FeedbackDialogMode;
      message: string;
      traceId?: string;
      autoCloseDuration?: number; // ms, default 3000 for success
    }
    ```
  - [x] 1.2 Template:
    - **Success**: icon `check_circle` (Material, màu `#4CAF50`), message, progress bar đếm ngược 3s, click-outside closes
    - **Error**: icon `error` (Material, màu `#F44336`), message, block `Mã lỗi: <traceId>` (ẩn nếu không có), nút "Xác nhận" màu primary
    - QT-03: dialog background trắng, text đen/xám đậm — không dùng màu background sặc sỡ
  - [x] 1.3 Auto-close logic cho success:
    ```typescript
    ngOnInit() {
      if (this.data.mode === 'success') {
        this.remaining = this.data.autoCloseDuration ?? 3000;
        this.timer = setInterval(() => {
          this.remaining -= 100;
          if (this.remaining <= 0) {
            clearInterval(this.timer);
            this.dialogRef.close();
          }
        }, 100);
      }
    }
    ngOnDestroy() { clearInterval(this.timer); }
    ```
  - [x] 1.4 CSS: progress bar `width: (remaining/total)*100%`, transition `width 100ms linear`

- [x] **Task 2: Tạo FeedbackDialogService** (AC: 1, 2, 4, 6)
  - [x] 2.1 Tạo `frontend/.../shared/services/feedback-dialog.service.ts`:
    ```typescript
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
          disableClose: true,    // bắt buộc user phải click Xác nhận
          panelClass: 'feedback-dialog-panel',
        });
      }

      private extractTraceId(err: unknown): string | undefined {
        if (!err || typeof err !== 'object') return undefined;
        const e = err as Record<string, unknown>;
        // HttpErrorResponse: e.error?.traceId (ProblemDetails)
        if (e['error'] && typeof e['error'] === 'object') {
          return (e['error'] as Record<string, unknown>)['traceId'] as string | undefined;
        }
        return e['traceId'] as string | undefined;
      }
    }
    ```
  - [x] 2.2 Đảm bảo `FeedbackDialogComponent` có `@Component({ ... })` đúng để `MatDialog.open()` hoạt động

- [x] **Task 3: Migration — Thay thế MatSnackBar trong 3 files hiện tại** (AC: 5)
  - [x] 3.1 `src/app/features/projects/components/board/board.ts`:
    - Xóa `inject(MatSnackBar)` và import `MatSnackBar`
    - Inject `FeedbackDialogService`
    - Line 126: `this.snackBar.open('Conflict...', ...)` → `this.feedbackDialog.error('Conflict: dữ liệu đã thay đổi, refresh để xem mới nhất')`
    - Line 148: `this.snackBar.open('Không thể tạo task...', ...)` → `this.feedbackDialog.error('Không thể tạo task. Thử lại?')`
  - [x] 3.2 `src/app/features/reports/components/budget/budget-filter-bar/budget-filter-bar.ts`:
    - Line 46: `snackBar.open('Đã sao chép liên kết!', ...)` → `this.feedbackDialog.success('Đã sao chép liên kết!')`
  - [x] 3.3 `src/app/features/reports/components/budget/budget-report/budget-report.ts`:
    - Line 65: `snackBar.open('Không thể xuất PDF.')` → `this.feedbackDialog.error('Không thể xuất PDF.', err)`
    - Line 75: `snackBar.open('Không thể xuất Excel.')` → `this.feedbackDialog.error('Không thể xuất Excel.', err)`
  - [x] 3.4 Verify bằng grep: `grep -r "MatSnackBar" src/` → 0 kết quả

- [x] **Task 4: Tests** (AC: 1–4)
  - [x] 4.1 `feedback-dialog.service.spec.ts` (Vitest + TestBed):
    - `extractTraceId` với HttpErrorResponse có `error.traceId` → trả đúng traceId
    - `extractTraceId` với plain Error object → trả `undefined`
    - `success()` gọi `MatDialog.open()` với mode='success', disableClose=false
    - `error()` gọi `MatDialog.open()` với mode='error', disableClose=true
    - `error()` gọi `console.error` với format đúng
  - [x] 4.2 `feedback-dialog.component.spec.ts` (Vitest, pure logic):
    - Success mode: `autoClose` timer chạy, `dialogRef.close()` sau 3s
    - Error mode: không có timer, dialog không tự đóng
    - `traceId` được render khi có, ẩn khi undefined
  - [x] 4.3 Build verification: `ng build` → 0 errors, 18/18 tests pass

- [x] **Task 5: Browser verification (QT-02)** (AC: 1–3)
  - [x] 5.1 Navigate `/reports/budget` → click Export PDF để trigger error dialog
  - [x] 5.2 Confirm: error dialog hiển thị, có "Xác nhận" button, không tự đóng
  - [x] 5.3 Click "Xác nhận" → dialog đóng, page về trạng thái bình thường
  - [x] 5.4 Screenshot: QT-03 — dialog background trắng, text đen, icon đỏ — không có màu nền sặc sỡ
  - [x] 5.5 Note: `navigator.clipboard` success dialog không test được qua Playwright (yêu cầu quyền `clipboard-write`); verified bằng code review và unit test

---

## Dev Notes

### File Structure Mới

```
frontend/project-management-web/src/app/shared/
├── components/
│   ├── confirm-dialog/           ← ĐÃ CÓ (xác nhận hành động destructive)
│   ├── conflict-dialog/          ← ĐÃ CÓ (409 conflict resolution)
│   └── feedback-dialog/          ← MỚI (CC-01)
│       ├── feedback-dialog.component.ts
│       └── feedback-dialog.component.spec.ts
└── services/
    ├── column-picker.service.ts  ← ĐÃ CÓ
    └── feedback-dialog.service.ts  ← MỚI (CC-01)
    └── feedback-dialog.service.spec.ts
```

### Phân biệt các Dialog

| Dialog | Khi nào dùng | Auto-close? |
|---|---|---|
| `ConfirmDialogComponent` | Xác nhận hành động destructive (delete, archive) | Không |
| `ConflictDialogComponent` | 409 conflict — chọn server vs local | Không |
| `FeedbackDialogComponent` (MỚI) | Mọi feedback success/error từ API | Success: 3s / Error: không |

### Lý do không dùng MatSnackBar

- Snackbar không block — user có thể miss error quan trọng
- Snackbar không hiển thị traceId rõ ràng (quá nhỏ)
- Snackbar không có log structured → khó debug
- Design: snackbar overlay ở bottom corner, dialog centered → phù hợp hơn với white/black design system (QT-03)

### TraceId — Nguồn lấy

Backend `GlobalExceptionMiddleware` + `CorrelationIdMiddleware` inject `X-Correlation-Id` header và trả về trong `ProblemDetails.traceId`. `ProblemDetails` model đã có field `traceId?: string` trong `shared/models/problem-details.model.ts`.

```typescript
// Từ HttpErrorResponse:
// err.error = { type: '...', title: '...', status: 500, traceId: 'xxx', ... }
const traceId = (err as HttpErrorResponse).error?.traceId;
```

### QT-04 (Bắt buộc từ CC-01)

**Sau khi story này hoàn thành, mọi dev agent PHẢI**:
- Inject `FeedbackDialogService` thay vì `MatSnackBar`
- Gọi `feedbackDialog.success(msg)` cho thành công
- Gọi `feedbackDialog.error(msg, err)` cho lỗi — `err` là HttpErrorResponse gốc để extract traceId
- KHÔNG import `MatSnackBarModule` hay `MatSnackBar` trong bất kỳ component nào mới

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Completion Notes List

- **FeedbackDialogComponent**: Standalone Angular component với `@if` syntax (Angular 17+), ChangeDetectorRef.markForCheck() cho OnPush, progress bar width animation qua `[style.width.%]`
- **FeedbackDialogService**: `extractTraceId` được expose dạng public để testable với TestBed; `satisfies FeedbackDialogData` type-safe
- **Migration**: 3 files — board.ts, budget-filter-bar.ts, budget-report.ts — đã xóa hoàn toàn MatSnackBar import + injection + MatSnackBarModule khỏi component decorator `imports[]`
- **Tests**: 18/18 pass — 8 pure logic tests (component) + 10 TestBed tests (service với mock MatDialog)
- **Build**: `ng build --configuration=development` → 0 errors
- **Browser QT-02**: Error dialog verified — "Đã xảy ra lỗi" title, error icon đỏ, message, nút "Xác nhận", không tự đóng
- **Browser QT-03**: Dialog background trắng (#ffffff), text đen/xám, không có màu nền sặc sỡ — screenshot xác nhận
- **Clipboard limitation**: `navigator.clipboard.writeText()` trong Playwright yêu cầu quyền `clipboard-write`; success dialog flow đã verified qua code review và unit test

### File List

**Mới tạo:**
- `frontend/project-management-web/src/app/shared/components/feedback-dialog/feedback-dialog.component.ts`
- `frontend/project-management-web/src/app/shared/components/feedback-dialog/feedback-dialog.component.spec.ts`
- `frontend/project-management-web/src/app/shared/services/feedback-dialog.service.ts`
- `frontend/project-management-web/src/app/shared/services/feedback-dialog.service.spec.ts`

**Sửa đổi:**
- `frontend/project-management-web/src/app/features/projects/components/board/board.ts` — migrate snackbar → feedbackDialog
- `frontend/project-management-web/src/app/features/reports/components/budget/budget-filter-bar/budget-filter-bar.ts` — migrate snackbar → feedbackDialog
- `frontend/project-management-web/src/app/features/reports/components/budget/budget-report/budget-report.ts` — migrate snackbar → feedbackDialog

### Change Log

- 2026-04-29: Implement CC-01 FeedbackDialogService — tạo component + service + migrate 3 files MatSnackBar + 18 tests
