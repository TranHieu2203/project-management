# Story 4.2: Overload warning UI (warn-only) + polling-friendly UX

Status: review

**Story ID:** 4.2
**Epic:** Epic 4 — Overload Warning (Standard + Predictive) + Cross-project Aggregation
**Sprint:** Sprint 5
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want thấy cảnh báo overload rõ ràng nhưng không bị chặn thao tác,
So that tôi vẫn làm việc được và hiểu rủi ro.

## Acceptance Criteria

1. **Given** overload detected
   **When** UI hiển thị cảnh báo
   **Then** hiển thị severity/tooltip "vì sao" (OL-01 hay OL-02, ngày/tuần nào)
   **And** KHÔNG chặn thao tác lưu — warn-only banner, không modal blocking

2. **Given** UI đang hiển thị overload data
   **When** polling refresh (mỗi 30s)
   **Then** data cập nhật silently — không flicker/reset UI state của người dùng

3. **Given** overload endpoint
   **When** UI hiển thị
   **Then** có timestamp "Cập nhật lúc: HH:mm:ss"
   **And** state loading hiển thị spinner nhỏ, không reset toàn bộ view
   **And** state error hiển thị message + nút "Retry"
   **And** state empty (không overload) hiển thị thông báo tích cực

---

## Tasks / Subtasks

- [x] **Task 1: Capacity store — thêm polling + lastUpdated**
  - [x] 1.1 Thêm `lastUpdated: string | null` vào `CapacityState`
  - [x] 1.2 Thêm action `startPolling` / `stopPolling` vào `capacity.actions.ts`
  - [x] 1.3 Cập nhật `loadOverloadSuccess` reducer: set `lastUpdated = new Date().toISOString()`
  - [x] 1.4 Cập nhật `capacity.effects.ts`: thêm `polling$` effect dùng `timer(0, 30000)` + `switchMap`
  - [x] 1.5 Export selector `selectLastUpdated` từ `capacity.reducer.ts`

- [x] **Task 2: Overload warning banner component**
  - [x] 2.1 Tạo `overload-warning-banner.ts` + `.html`: compact banner hiển thị khi `hasOverload = true`
  - [x] 2.2 Banner hiển thị: số ngày OL-01, số tuần OL-02, icon cảnh báo
  - [x] 2.3 Tooltip/expand chi tiết: danh sách ngày/tuần bị overload với số giờ
  - [x] 2.4 Banner là warn-only — không disable/block bất kỳ thao tác nào

- [x] **Task 3: Enhance overload-dashboard với polling UX**
  - [x] 3.1 Thêm "Cập nhật lúc: {{lastUpdated}}" label (format HH:mm:ss)
  - [x] 3.2 Polling spinner nhỏ (không thay thế toàn bộ content) khi background refresh
  - [x] 3.3 Error state với "Retry" button dispatch lại loadOverload
  - [x] 3.4 Empty state message khi không có overload
  - [x] 3.5 Dispatch `startPolling` khi form submit, `stopPolling` khi component destroy

- [x] **Task 4: Build verification**
  - [x] 4.1 `ng build` → 0 errors, 0 warnings

---

## Dev Notes

### Polling pattern (Story 4.1 store đã có, extend thêm)

```typescript
// capacity.actions.ts — thêm:
'Start Polling': props<{ resourceId: string; dateFrom: string; dateTo: string }>(),
'Stop Polling': emptyProps(),

// capacity.effects.ts — polling effect:
startPolling$ = createEffect(() =>
  this.actions$.pipe(
    ofType(CapacityActions.startPolling),
    switchMap(({ resourceId, dateFrom, dateTo }) =>
      timer(0, 30000).pipe(
        takeUntil(this.actions$.pipe(ofType(CapacityActions.stopPolling))),
        switchMap(() =>
          this.api.getResourceOverload(resourceId, dateFrom, dateTo).pipe(
            map(result => CapacityActions.loadOverloadSuccess({ result })),
            catchError(err => of(CapacityActions.loadOverloadFailure({ error: err?.message ?? 'Lỗi.' })))
          )
        )
      )
    )
  )
);
```

### State — thêm lastUpdated

```typescript
export interface CapacityState {
  result: ResourceOverloadResult | null;
  loading: boolean;
  error: string | null;
  lastUpdated: string | null;  // ISO string
}

// Reducer:
on(CapacityActions.loadOverloadSuccess, (state, { result }) => ({
  ...state, loading: false, result, lastUpdated: new Date().toISOString(), error: null
})),
```

### Overload warning banner

Component `overload-warning-banner`:
- `@Input() result: ResourceOverloadResult | null`
- `@Input() loading: boolean`
- `@Input() lastUpdated: string | null`
- Hiển thị khi `result?.hasOverload`
- Dùng `MatTooltipModule` cho chi tiết
- **KHÔNG dùng NgRx trong banner component** — nhận input từ parent (dùng được ở nhiều nơi)

```html
<!-- Banner layout -->
<div *ngIf="result?.hasOverload" class="overload-banner">
  <mat-icon color="warn">warning</mat-icon>
  <span>Overload: {{ overloadedDays }} ngày OL-01, {{ overloadedWeeks }} tuần OL-02</span>
  <span class="detail-link" [matTooltip]="tooltipText">Chi tiết</span>
  <span class="last-updated" *ngIf="lastUpdated">Cập nhật lúc: {{ lastUpdated | date:'HH:mm:ss' }}</span>
  <mat-spinner *ngIf="loading" diameter="16"></mat-spinner>
</div>
```

### Polling UX — không flicker

Khi polling refresh:
- `loading` chỉ set `true` khi lần đầu load (result = null)
- Background refresh: dùng separate `refreshing: boolean` state, hoặc giữ `loading = false` khi result đã có
- Thực tế: dùng `loading` chỉ để hiển thị spinner nhỏ trong banner, không ẩn content

### Patterns đã có — KHÔNG viết lại

| Pattern | Source |
|---|---|
| NgRx `createActionGroup`, `createEffect`, `ofType` | Story 4.1 + Story 3.x |
| `timer(0, interval)` polling | Architecture: `capacity.effects.ts ← polling interval via timer()` |
| `takeUntil` + stop action | Common NgRx polling pattern |
| `@Input()` component (no NgRx) | Story 2.5 audit-log |
| `DatePipe` với `'HH:mm:ss'` format | Angular built-in |

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Fixed `OverloadWarningBannerComponent is not used within the template` by rewriting `overload-dashboard.html` to include `<app-overload-warning-banner>`.
- `*ngIf` deprecation hints (severity Hint only) — non-blocking, consistent with entire codebase.

### Completion Notes List

- Polling uses `timer(0, 30000)` + `takeUntil(stopPolling$)` + outer `switchMap` to cancel in-flight if `startPolling` re-dispatched.
- No-flicker: `loading: state.result === null` — spinner only on first load; background refresh silent.
- `OverloadWarningBannerComponent` is purely `@Input()`-driven (no NgRx inside) — reusable anywhere.

### File List

- `frontend/.../capacity/store/capacity.actions.ts` — added `startPolling`, `stopPolling`
- `frontend/.../capacity/store/capacity.reducer.ts` — added `lastUpdated`, no-flicker `loading` logic
- `frontend/.../capacity/store/capacity.effects.ts` — added `startPolling$` polling effect
- `frontend/.../capacity/components/overload-warning-banner/overload-warning-banner.ts` (new)
- `frontend/.../capacity/components/overload-warning-banner/overload-warning-banner.html` (new)
- `frontend/.../capacity/components/overload-dashboard/overload-dashboard.ts` — integrated banner, OnDestroy
- `frontend/.../capacity/components/overload-dashboard/overload-dashboard.html` — rewritten with banner, error, empty state
