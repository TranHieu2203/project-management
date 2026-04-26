# Story 1.9: Nav Sidebar Toggle Menu

Status: review

## Story

As a PM,
I want ẩn/hiện sidebar navigation bằng một nút toggle,
so that tôi có thể tối đa hóa không gian làm việc khi cần, đặc biệt trên màn hình Gantt view, và trạng thái sidebar được ghi nhớ giữa các phiên làm việc.

## Acceptance Criteria

1. **Given** sidebar đang mở (expanded 220px)
   **When** PM click nút toggle
   **Then** sidebar thu gọn về 56px (icon-only mode), labels bị ẩn, brand name bị ẩn
   **And** transition diễn ra mượt (CSS transition ~200ms)

2. **Given** sidebar đang thu gọn (collapsed 56px)
   **When** PM click nút toggle
   **Then** sidebar mở rộng về 220px (full mode), labels và brand name hiện lại
   **And** transition diễn ra mượt (CSS transition ~200ms)

3. **Given** sidebar thu gọn (icon-only)
   **When** PM hover vào một nav item
   **Then** `matTooltip` hiển thị label tương ứng (vì label bị ẩn)

4. **Given** PM đã collapse sidebar
   **When** PM reload trang hoặc mở tab mới
   **Then** sidebar vẫn thu gọn (state được persist qua `localStorage` key `pm_sidenav_expanded`)

5. **Given** PM đã expand sidebar
   **When** PM reload trang
   **Then** sidebar vẫn mở rộng (persist qua localStorage)

6. **Given** sidebar đang ở bất kỳ trạng thái nào
   **When** PM navigate giữa các route
   **Then** sidebar state không thay đổi (toggle state độc lập với routing)

## Tasks / Subtasks

- [x] Task 1: Thêm toggle state vào AppShellComponent (AC: #1, #2, #4, #5)
  - [x] 1.1 Thêm `sidenavExpanded = signal<boolean>(true)` — đọc initial value từ `localStorage.getItem('pm_sidenav_expanded')` (default `true` nếu chưa có)
  - [x] 1.2 Tạo method `toggleSidenav()`: flip signal, ghi `localStorage.setItem('pm_sidenav_expanded', value.toString())`
  - [x] 1.3 Inject `ChangeDetectorRef` **không cần** — signal tự trigger OnPush update

- [x] Task 2: Cập nhật template `app-shell.html` (AC: #1, #2, #3)
  - [x] 2.1 Thêm `[class.shell-sidenav--collapsed]="!sidenavExpanded()"` vào `<mat-sidenav>`
  - [x] 2.2 Thêm nút toggle vào `sidenav-header` (cuối header, sau brand-name)
  - [x] 2.3 Thêm `[matTooltip]="item.label"` và `[matTooltipDisabled]="sidenavExpanded()"` vào mỗi nav-link
  - [x] 2.4 Thêm `matTooltipPosition="right"` để tooltip hiển thị bên phải icon

- [x] Task 3: Cập nhật `app-shell.scss` (AC: #1, #2)
  - [x] 3.1 Thêm CSS cho `collapsed` state (width: 56px, ẩn labels)
  - [x] 3.2 Điều chỉnh `.sidenav-header` khi collapsed: căn giữa nút toggle
  - [x] 3.3 Điều chỉnh `.nav-link` khi collapsed: `justify-content: center; padding: 9px 0; gap: 0`
  - [x] 3.4 Nút toggle (`.toggle-btn`): màu `rgba(255,255,255,0.38)`, hover sáng hơn
  - [x] 3.5 Fix content area expansion: global CSS override `mat-sidenav-content` margin-left với transition 0.2s

- [x] Task 4: Viết Vitest spec (AC: #1–#5)
  - [x] 4.1 Test `toggleSidenav()` flip signal từ `true` → `false` và gọi `localStorage.setItem`
  - [x] 4.2 Test initial state đọc từ `localStorage` (`'false'` → `sidenavExpanded()` là `false`)
  - [x] 4.3 Test signal state transitions (7 tests pass via direct class instantiation)
  - [x] 4.4 DOM render tests skipped — project test infra cần `@analogjs/vitest-angular` để test component templates

## Dev Notes

### Trạng thái hiện tại — ĐỌC TRƯỚC KHI CODE

`AppShellComponent` ĐÃ TỒN TẠI tại `src/app/core/shell/app-shell.ts`:
- `mat-sidenav mode="side" opened` — luôn mở, không có toggle
- Width cứng 220px trong SCSS
- Không có state management cho sidenav

**Story này KHÔNG tạo component mới — chỉ mở rộng AppShellComponent đã có.**

### Cách tiếp cận: CSS-class collapse (KHÔNG dùng `mat-sidenav [opened]`)

**Tại sao không dùng `[opened]` binding:**
- Khi `opened=false`, Angular Material ẩn sidenav hoàn toàn (cả content area bị shift)
- Chúng ta muốn sidenav luôn visible, chỉ thu hẹp width → dùng CSS class approach

**Pattern đúng:**
```typescript
// Luôn giữ opened=true
// Điều khiển via CSS class
[class.shell-sidenav--collapsed]="!sidenavExpanded()"
```

### State Management: Angular Signal (KHÔNG dùng NgRx)

Toggle state là **local UI state** của AppShellComponent — không cần share qua NgRx store. Dùng `signal()` là đúng pattern Angular 17+.

```typescript
import { signal } from '@angular/core';

// Trong class AppShellComponent:
readonly sidenavExpanded = signal<boolean>(
  localStorage.getItem('pm_sidenav_expanded') !== 'false'
);
```

> `!== 'false'` để default là `true` khi key chưa tồn tại.

### Transition CSS

CSS `transition: width 0.2s ease` đặt trên `.shell-sidenav` (luôn active). Khi class thay đổi, trình duyệt tự animate.

**Quan trọng:** Đừng dùng `overflow: hidden` trong lúc transition vì sẽ clip content — thay vào đó dùng `opacity: 0` + `pointer-events: none` cho `.nav-label`.

### matTooltip cho collapsed state

```html
<a class="nav-link"
   [routerLink]="item.route"
   routerLinkActive="nav-link--active"
   [matTooltip]="item.label"
   [matTooltipDisabled]="sidenavExpanded()"
   matTooltipPosition="right">
```

`MatTooltipModule` đã được import vào `AppShellComponent` từ trước.

### Không cần backend

Story này là **100% frontend** — không có API call, không có NgRx action, không có BE changes.

### Project Structure Notes

| File | Hành động |
|---|---|
| `src/app/core/shell/app-shell.ts` | Thêm signal + toggleSidenav() + class binding |
| `src/app/core/shell/app-shell.html` | Thêm toggle button + collapsed class + tooltip bindings |
| `src/app/core/shell/app-shell.scss` | Thêm `--collapsed` modifier + transition |
| `src/app/core/shell/app-shell.spec.ts` | Tạo mới (hoặc cập nhật nếu đã có) |

### References

- [Source: architecture.md#AD-02] NgRx chỉ dùng cho shared cross-component state — local UI state dùng signal
- [Source: PRD FR4] "Người dùng có thể ẩn/hiện sidebar navigation (toggle menu)"
- [Source: PRD FR5] "Hệ thống ghi nhớ trạng thái sidebar giữa các phiên làm việc"
- [Source: app-shell.ts] `MatTooltipModule` đã import sẵn
- [Source: app-shell.scss] `width: 220px` hiện tại, `$navy` color palette

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

### File List

- `src/app/core/shell/app-shell.ts` — signal + toggleSidenav()
- `src/app/core/shell/app-shell.html` — toggle button + class binding + tooltip bindings
- `src/app/core/shell/app-shell.scss` — collapsed state CSS
- `src/app/core/shell/app-shell.spec.ts` — 7 unit tests (direct class instantiation)
- `src/styles.scss` — global mat-sidenav-content margin override for content expansion
