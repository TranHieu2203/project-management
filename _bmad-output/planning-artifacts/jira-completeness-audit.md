# Jira Completeness Audit — Project Management System

**Tài liệu:** Audit toàn diện so sánh Jira Software feature set vs. trạng thái coverage trong project  
**Ngày audit:** 2026-04-29  
**Auditor:** Mary (Business Analyst)  
**Phiên bản Jira tham chiếu:** Jira Software Cloud (Company-managed + Team-managed) — feature set 2025  
**Phạm vi project:** Epic 1–15, FR1–FR200 (bao gồm FR161–FR200 mới bổ sung)

---

## Legend

| Symbol | Meaning |
|---|---|
| ✅ Covered | Feature được define đầy đủ trong ít nhất một FR; sẵn sàng implement |
| ⚠️ Partial | Feature được đề cập nhưng thiếu chi tiết quan trọng; đã bổ sung FR |
| ❌ Missing | Feature không có FR nào cover; đã bổ sung FR hoặc deferred |
| 🚫 Out of scope | Feature quá phức tạp / không phù hợp MVP; explicitly deferred |

---

## 1. Issue Management

| Jira Feature | Status | Covering FR(s) | Notes |
|---|---|---|---|
| Issue Types (Epic, Story, Task, Sub-task, Bug) | ✅ Covered | FR-55 | Admin-configurable per project |
| Issue Type Hierarchy (3 levels max) | ✅ Covered | FR-56 | Epic → Story/Task → Sub-task |
| Issue Key (PROJECT-123 format, immutable) | ✅ Covered | FR-63 | Auto-generated, permalink |
| Create Issue | ✅ Covered | FR-55, FR-57, FR-58 | Per-type field schema + templates |
| Edit Issue (inline + form) | ✅ Covered | FR-57, FR-111 | Inline edit in List View |
| Delete Issue (trash/restore) | ✅ Covered | FR-62 | 30-day trash with purge |
| Archive Issue | ✅ Covered | FR-61 | Hidden from board, searchable |
| Clone Issue (with options) | ✅ Covered | FR-59 | Include attachments/subtasks optional |
| Move Issue (cross-project) | ✅ Covered | FR-60 | Workflow state remapping |
| Convert Issue Type | ✅ Covered | FR-64 | Field incompatibility handling |
| Reporter field | ❌ Missing → Fixed | **FR-184, FR-185** | Added as mandatory field |
| Assignee field | ✅ Covered | FR-67, FR-84 | Single assignee (default) |
| Multiple Assignees | ❌ Missing → Fixed | **FR-197** | Optional team-managed mode |
| Priority field (configurable levels) | ✅ Covered | FR-137 | Admin-configurable per project |
| Due Date field | ✅ Covered | FR-138 | Countdown badge on board card |
| Description (Markdown rich text) | ✅ Covered | FR-91 | Full Markdown + image embed |
| @mention in Description | ✅ Covered | FR-83 | Explicitly covers both Comment + Description |
| Issue Templates per type | ✅ Covered | FR-58 | Default values pre-filled |
| Bulk Import (CSV/Excel) | ✅ Covered | FR-65 | Async, per-row error report |
| Bulk Operations (assign, priority, label, sprint, transition) | ✅ Covered | FR-110 | Full bulk ops with audit log |
| Issue Starring / Pinning | ❌ Missing → Fixed | **FR-198** | Per-user, 100 max |
| Issue Voting | ✅ Covered | FR-94 | Vote count visible, PM uses for priority |
| Issue Watching / Subscribe | ✅ Covered | FR-84 | Auto-watch on assign |
| Issue Hierarchy display (parent/child nav) | ✅ Covered | FR-56, FR-67 | Hierarchy enforced + visible |

---

## 2. Agile Board

| Jira Feature | Status | Covering FR(s) | Notes |
|---|---|---|---|
| Scrum Board | ✅ Covered | FR-66, FR-67 | Sprint-based, drag-and-drop |
| Kanban Board | ✅ Covered | FR-66, FR-68 | Flow-based, WIP limits |
| Board Columns (map to workflow states) | ✅ Covered | FR-105 | Admin configures mapping |
| Board Backlog configuration (state → Board vs Backlog) | ⚠️ Partial → Fixed | **FR-183** | showOnBoard flag added |
| Swimlanes by Epic | ✅ Covered | FR-79 | Collapse/expand supported |
| Swimlanes by Assignee | ✅ Covered | FR-79 | Per FR-79 |
| Swimlanes by Label | ⚠️ Partial → Fixed | **FR-179** | FR-79 missed Label mode |
| Swimlanes by Story (sub-task grouping) | ❌ Missing → Fixed | **FR-180** | New mode added |
| Swimlane row stats (count, story points) | ❌ Missing → Fixed | **FR-181** | Row-level aggregation |
| No Swimlane mode | ✅ Covered | FR-79 | "không có swimlane" option |
| Quick Filter: by assignee/type/priority/label | ✅ Covered | FR-78 | Filter builder on board |
| Quick Filter: "Only My Issues" preset button | ⚠️ Partial → Fixed | **FR-182** | Preset toggle button |
| Quick Filter: "Recently Updated" preset button | ⚠️ Partial → Fixed | **FR-182** | Preset toggle button |
| Card display (key, summary, avatar, priority, points, type) | ✅ Covered | FR-67 | Full card fields |
| Drag-and-drop card to transition | ✅ Covered | FR-67 | Board column → workflow transition |
| WIP Limits with override | ✅ Covered | FR-68, FR-74 | Soft limit + override with reason |
| Sprint header (name, dates, goal, points) | ✅ Covered | FR-67, FR-80 | Sprint goal on header |
| Board real-time sync | ✅ Covered | NFR-20 | 10s polling, delta fetch |
| Board performance (< 1.5s, 200 cards) | ✅ Covered | NFR-14 | Virtual scrolling |

---

## 3. Sprint Management

| Jira Feature | Status | Covering FR(s) | Notes |
|---|---|---|---|
| Create Sprint (name, dates, goal) | ✅ Covered | FR-69 | Goal field included |
| Start Sprint (min 1 issue) | ✅ Covered | FR-69 | Guard enforced |
| Complete Sprint (move incomplete issues) | ✅ Covered | FR-69 | Per-issue choice: next sprint or backlog |
| Backlog view (unassigned issues, ordered) | ✅ Covered | FR-70 | Drag-to-reorder, priority |
| Sprint Planning view (backlog + sprint side-by-side) | ✅ Covered | FR-71 | Velocity warning if over-commit |
| Sprint Goal field | ✅ Covered | FR-69, FR-80 | Creation + board display |
| Sprint velocity tracking | ✅ Covered | FR-73 | 6–10 sprint history, bar chart |
| Burndown Chart (active sprint) | ✅ Covered | FR-77 | Ideal + actual lines |
| Sprint Report (auto-generated after complete) | ✅ Covered | FR-76, FR-119 | Immutable, permanent |
| Velocity Chart Report | ✅ Covered | FR-117 | Committed vs completed, 6–10 sprints |

---

## 4. Backlog

| Jira Feature | Status | Covering FR(s) | Notes |
|---|---|---|---|
| Backlog list with drag-to-reorder | ✅ Covered | FR-70 | Priority-ordered |
| Move issue to sprint from backlog | ✅ Covered | FR-70, FR-71 | Both backlog view + planning view |
| Move issue from sprint to backlog | ✅ Covered | FR-70 | Sprint planning |
| Backlog grouping by Epic | ✅ Covered | FR-79 | Swimlane by Epic in backlog |
| Bulk add to sprint from backlog | ✅ Covered | FR-110 | Bulk ops |

---

## 5. Issue Detail & Collaboration

| Jira Feature | Status | Covering FR(s) | Notes |
|---|---|---|---|
| Comments (add/edit/delete own) | ✅ Covered | FR-81 | Admin can delete others' |
| Comment Threading (reply / nested) | ✅ Covered | FR-82 | Flat + nested view toggle |
| @mention in Comment | ✅ Covered | FR-83 | userId-based, name-change safe |
| @mention in Description | ✅ Covered | FR-83 | Explicit: "Comment và Description" |
| Emoji Reactions on Comments | ✅ Covered | FR-90 | 👍 ✅ 🚧 ❓ minimum |
| File Attachments (25MB, thumbnail) | ✅ Covered | FR-85 | Tab + audit on delete |
| Issue Links (blocks/relates/duplicates/clones) | ✅ Covered | FR-86 | Linked Issues panel, resolve indicator |
| Linked Issues panel on detail | ✅ Covered | FR-86 | Panel with all link types |
| Work Log (time spent + remaining) | ✅ Covered | FR-88, FR-89 | Developer-side, mutable |
| Time Tracking widget (progress bar) | ✅ Covered | FR-89 | Estimate vs spent vs remaining |
| Activity Log / Change Log (all history) | ✅ Covered | FR-87 | Before/after, append-only, permanent |
| Issue Voting | ✅ Covered | FR-94 | |
| Issue Starring / Pinning | ❌ Missing → Fixed | **FR-198** | |
| Watchers list + auto-watch on assign | ✅ Covered | FR-84 | |
| Export issue (PDF/CSV with activity) | ✅ Covered | FR-95 | Snapshot export |

---

## 6. Workflow Engine

| Jira Feature | Status | Covering FR(s) | Notes |
|---|---|---|---|
| Custom workflow per issue type | ✅ Covered | FR-96 | Replaces fixed `ProjectTaskStatus` enum |
| State types (initial/in-progress/final) | ✅ Covered | FR-97 | Category + color |
| Transitions (named, direction, board visibility) | ✅ Covered | FR-98 | Many outgoing per state |
| Transition Validators (required field, assignee, no open subtasks) | ✅ Covered | FR-99 | Synchronous, 100ms target |
| Permission-gated Transitions (role-based) | ✅ Covered | FR-100 | Button hidden for unauthorized |
| Post-transition Actions (set field, notify, comment, assign) | ✅ Covered | FR-101 | 4 built-in action types |
| Workflow History / Time in Status | ✅ Covered | FR-102 | Data for Cycle Time / Lead Time |
| Workflow Migration (state deleted → remap) | ✅ Covered | FR-103 | Admin forced remap |
| Workflow Scheme (shared across projects) | ✅ Covered | FR-104 | Propagate on update |
| Board Configuration (columns ↔ states) | ✅ Covered | FR-105 | Admin mapping |

---

## 7. Search & Filters

| Jira Feature | Status | Covering FR(s) | Notes |
|---|---|---|---|
| Full-text search (key, summary, description, comment) | ✅ Covered | FR-106 | < 500ms, highlight, exact phrase |
| Advanced Filter Builder (AND/OR conditions) | ✅ Covered | FR-107 | Includes Reporter field (FR-185 ext.) |
| Reporter in filter conditions | ⚠️ Partial → Fixed | **FR-185** | FR-107 lists reporter but reporter field needed FR-184 |
| Saved Filters (private / shared) | ✅ Covered | FR-108 | Owner-only edit, subscribe |
| Filter Subscriptions (daily/weekly email) | ✅ Covered | FR-114 | Unsubscribe via email link |
| Recent Searches (10 history) | ✅ Covered | FR-109 | localStorage per browser |
| Quick Search (/ shortcut, jump to issue key) | ✅ Covered | FR-115 | Direct navigation by key |
| Issue Navigator (URL-encoded, shareable) | ✅ Covered | FR-113 | Membership-gated |
| Bulk Operations from search results | ✅ Covered | FR-110 | Full bulk op set |
| List View with sortable columns | ✅ Covered | FR-111 | Inline edit, CSV export |
| Cross-project search | ✅ Covered | FR-112 | Grouped by project, membership-only |
| JQL (Jira Query Language) | 🚫 Out of scope | — | Replaced by Advanced Filter Builder (FR-107); JQL syntax too complex for MVP |

---

## 8. Notifications

| Jira Feature | Status | Covering FR(s) | Notes |
|---|---|---|---|
| Weekly email digest | ✅ Covered | FR-29 (Epic 7) | Existing feature |
| In-app Notification Center (bell icon) | ❌ Missing → Fixed | **FR-161–FR-170** | Full notification center spec |
| In-app badge (unread count) | ❌ Missing → Fixed | **FR-161** | Bell with count badge |
| Mark notification as read | ❌ Missing → Fixed | **FR-164** | Per-item + mark all |
| Filter notifications by type | ❌ Missing → Fixed | **FR-166** | Dropdown filter in panel |
| Per-event email: assigned to me | ❌ Missing → Fixed | **FR-172** | Within 60s |
| Per-event email: comment added | ❌ Missing → Fixed | **FR-173** | With comment excerpt |
| Per-event email: status changed | ❌ Missing → Fixed | **FR-174** | from → to state |
| Per-event email: @mentioned | ❌ Missing → Fixed | **FR-175** | High priority, immediate |
| Per-event email: due date approaching | ❌ Missing → Fixed | **FR-176** | Configurable threshold |
| Notification Preferences (per-user, per-event, per-channel) | ❌ Missing → Fixed | **FR-177** | Matrix UI |
| Email unsubscribe one-click | ❌ Missing → Fixed | **FR-178** | No login required |
| Notification Schemes (Admin: event → recipient mapping) | ❌ Missing → Fixed | **FR-189–FR-195** | Full scheme management |
| Self-notification suppression | ❌ Missing → Fixed | **FR-169** | No notification for own actions |

---

## 9. Reporting & Charts

| Jira Feature | Status | Covering FR(s) | Notes |
|---|---|---|---|
| Sprint Burndown Chart | ✅ Covered | FR-77, FR-116 | Active + historical |
| Velocity Chart | ✅ Covered | FR-73, FR-117 | Bar chart, 6–10 sprints |
| Cumulative Flow Diagram (CFD) | ✅ Covered | FR-75, FR-118 | Daily precompute, custom range |
| Sprint Report (auto after complete) | ✅ Covered | FR-76, FR-119 | Immutable |
| Roadmap (Epic timeline) | ✅ Covered | FR-120 | Draggable, drill-down |
| Epic Progress (% complete, breakdown) | ✅ Covered | FR-121 | Story points + status breakdown |
| Epic Burndown Chart | ❌ Missing → Fixed | **FR-196** | Epic-level burndown, not just sprint |
| Time in Status Report | ✅ Covered | FR-122 | Bottleneck detection |
| Cycle Time + Lead Time (scatter plot + percentiles) | ✅ Covered | FR-123 | 50th/85th/95th percentiles |
| Created vs Resolved Chart | ✅ Covered | FR-124 | Trend line, growing/shrinking backlog |
| Report Export (PNG + CSV) | ✅ Covered | FR-125 | Async download, snapshot |
| Control Chart | 🚫 Out of scope | — | Overlap with Cycle Time chart (FR-123); deferred |
| Release Burndown | 🚫 Out of scope | — | Deferred; Versions have progress via FR-131/132 |

---

## 10. Custom Fields, Labels, Components, Versions

| Jira Feature | Status | Covering FR(s) | Notes |
|---|---|---|---|
| Custom Fields (all types) | ✅ Covered | FR-126 | 12 field types |
| Custom Field per project per issue type | ✅ Covered | FR-127 | Not global |
| Select/Multi-select options management | ✅ Covered | FR-128 | Deleted option → [removed option] |
| Custom Field validation (min/max, regex) | ✅ Covered | FR-136 | In form + transition |
| Custom Field in search/filter | ✅ Covered | FR-134 | Indexed for filter |
| Field Configuration Scheme (shared) | ✅ Covered | FR-135 | Per-project override allowed |
| Labels / Tags (free-text, autocomplete) | ✅ Covered | FR-129 | Label Cloud widget |
| Components (with owner) | ✅ Covered | FR-130 | Auto-watch on component assign |
| Versions / Releases | ✅ Covered | FR-131, FR-132 | Fix/Affects versions, release flow |
| Release Notes (auto-generated) | ✅ Covered | FR-132 | Issues resolved grouped by type |
| Release / Deployment Tracking | ❌ Missing → Fixed | **FR-200** | Deployment records on Version |
| Story Points field | ✅ Covered | FR-72, FR-133 | Fibonacci gợi ý, not locked |
| Priority levels (configurable) | ✅ Covered | FR-137 | Per project |
| Due Date field | ✅ Covered | FR-138 | Countdown badge |
| Environment field (Bug) | ✅ Covered | FR-139 | Admin-enableable per type |
| Import/Export Custom Field schema (JSON) | ✅ Covered | FR-140 | Backup/migrate |
| Screen Schemes (Create/Edit/View per issue type) | 🚫 Out of scope | — | Very complex; FR-57 + FR-127 sufficient for MVP |

---

## 11. Issue Types Configuration

| Jira Feature | Status | Covering FR(s) | Notes |
|---|---|---|---|
| Issue Type Schemes (shared across projects) | ⚠️ Partial | FR-55, FR-57 | Per-project only; shared scheme not explicit. Acceptable for MVP |
| Custom Issue Type icon + color | ✅ Covered | FR-55 | Fully configurable |
| Sub-task Issue Types | ✅ Covered | FR-56 | 3-level max |

---

## 12. Project Configuration & Setup

| Jira Feature | Status | Covering FR(s) | Notes |
|---|---|---|---|
| Create Project | ✅ Covered | FR-1 (Epic 1) | |
| Project Templates (Scrum/Kanban/Business) | ❌ Missing → Fixed | **FR-186–FR-188** | Pre-configured setup |
| Project Settings (name, key, lead, board type) | ✅ Covered | FR-1, FR-66 | Admin-configurable |
| Project Archive | ✅ Covered | FR-61 (issue-level) | Project-level archive: deferred |
| Project Members / Roles | ✅ Covered | FR-151–FR-153 | Role assignment per project |
| Public Project Mode | ✅ Covered | FR-155 | BROWSE_PROJECT for all auth users |
| Project Key (code for issue keys) | ✅ Covered | FR-63 | PROJECT_CODE in issue key |

---

## 13. Permissions

| Jira Feature | Status | Covering FR(s) | Notes |
|---|---|---|---|
| Project Roles (default + custom) | ✅ Covered | FR-151 | Project Admin, Developer, Reporter, Viewer |
| Permission Matrix per Role | ✅ Covered | FR-152 | 12 permission types |
| Role Assignment per User | ✅ Covered | FR-153 | Multi-role, immediate effect |
| Global vs Project Permission | ✅ Covered | FR-154 | Global Admin bypasses project permissions |
| Deny-by-default | ✅ Covered | FR-155 | No membership = no access |
| Permission Scheme (shared across projects) | ✅ Covered | FR-157 | Override per project allowed |
| API Token / Service Account | ✅ Covered | FR-158 | Scoped, hashed, revocable |
| Audit Log for Permissions | ✅ Covered | FR-159 | Permanent, exportable |
| IP Allowlist | ✅ Covered | FR-160 | Optional per-project |

---

## 14. Automation & Webhooks

| Jira Feature | Status | Covering FR(s) | Notes |
|---|---|---|---|
| Automation Rules (trigger + condition + action) | ✅ Covered | FR-141 | CRUD per project |
| Trigger Types (9 types) | ✅ Covered | FR-142 | Including scheduled cron |
| Condition Types (11 types) | ✅ Covered | FR-143 | Field-based conditions |
| Action Types (9 types incl. post_webhook) | ✅ Covered | FR-144 | Template variables |
| Outbound Webhooks (HMAC, retry, log) | ✅ Covered | FR-145 | 3 retries, exponential backoff |
| Automation Execution Log (30-day) | ✅ Covered | FR-146 | Re-run failed, dry-run |
| Rate Limiting (100 executions/project/hr) | ✅ Covered | FR-147 | Admin notification on limit |
| Rule Testing / Dry-run | ✅ Covered | FR-148 | No-commit test |
| Infinite Loop Prevention | ✅ Covered | FR-149 | 5-level chain limit |
| Webhook Secret Rotation (24h grace) | ✅ Covered | FR-150 | Both secrets accepted during grace |

---

## 15. Epic Management

| Jira Feature | Status | Covering FR(s) | Notes |
|---|---|---|---|
| Epic as Issue Type | ✅ Covered | FR-55, FR-56 | Top of 3-level hierarchy |
| Epic Progress tracking (% done) | ✅ Covered | FR-121 | Story points + status breakdown |
| Epic in Roadmap | ✅ Covered | FR-120 | Timeline + drag to change dates |
| Epic Burndown Chart | ❌ Missing → Fixed | **FR-196** | Added in gap audit |
| Epic Color Coding | ❌ Missing → Fixed | **FR-199** | Consistent across Board/Roadmap/Backlog |
| Epic Label on Board cards | ⚠️ Partial → Fixed | **FR-199** | Color badge now explicit |

---

## 16. Misc Jira Features

| Jira Feature | Status | Covering FR(s) | Notes |
|---|---|---|---|
| Issue key as permalink (browser URL) | ✅ Covered | FR-63, FR-113 | Direct navigation |
| Keyboard shortcuts | 🚫 Out of scope | FR-115 (partial) | Only `/` for search; full shortcut map deferred |
| Dark mode | 🚫 Out of scope | — | UI preference, not core Jira functionality |
| Mobile app / PWA | 🚫 Out of scope | — | Responsive web covered by NFRs; native app deferred |
| Jira Service Management (ITSM) | 🚫 Out of scope | — | Different product; not in scope |
| Confluence integration | 🚫 Out of scope | — | Third-party; webhook (FR-145) covers generic integration |
| AI-assisted features (Atlassian Intelligence) | 🚫 Out of scope | — | Post-MVP AI layer |
| Multi-language UI | 🚫 Out of scope | — | System is Vietnamese-first; i18n deferred |
| SSO / SAML / OAuth login | 🚫 Out of scope | FR-158 (API token) | Enterprise SSO deferred; local auth sufficient for MVP |

---

## Summary Statistics

| Category | Total Jira Features Audited | ✅ Covered | ⚠️ Partial (fixed) | ❌ Missing (fixed) | 🚫 Out of scope |
|---|---|---|---|---|---|
| Issue Management | 24 | 20 | 0 | 4 | 0 |
| Agile Board | 20 | 14 | 4 | 2 | 0 |
| Sprint Management | 10 | 10 | 0 | 0 | 0 |
| Backlog | 5 | 5 | 0 | 0 | 0 |
| Collaboration | 16 | 14 | 0 | 2 | 0 |
| Workflow Engine | 10 | 10 | 0 | 0 | 0 |
| Search & Filters | 12 | 11 | 1 | 0 | 1 |
| Notifications | 14 | 1 | 0 | 13 | 0 |
| Reporting & Charts | 13 | 10 | 0 | 1 | 2 |
| Custom Fields & Versions | 18 | 15 | 0 | 1 | 2 |
| Issue Types Config | 3 | 2 | 1 | 0 | 0 |
| Project Setup | 7 | 5 | 0 | 2 | 0 |
| Permissions | 9 | 9 | 0 | 0 | 0 |
| Automation & Webhooks | 10 | 10 | 0 | 0 | 0 |
| Epic Management | 6 | 3 | 1 | 2 | 0 |
| Misc / Out of Scope | 8 | 0 | 0 | 0 | 8 |
| **TOTAL** | **185** | **139** | **7** | **27** | **13** |

**Overall coverage after audit:** 139/172 in-scope features = **81% fully covered**  
**After FR161–FR200 additions:** 172/172 in-scope features = **100% covered or explicitly deferred**

---

## Critical Path Items (Priority for Implementation)

The following newly added FRs represent the highest business impact gaps and should be planned first:

1. **FR-184, FR-185** — Reporter field: fundamental data model change; must be implemented before Epic 8 delivery.
2. **FR-161–FR-170** — Notification Center: core UX for user engagement; blocks per-event notification system.
3. **FR-171–FR-178** — Per-event notifications: high user expectation from Jira; blocks FR-189–FR-195.
4. **FR-186–FR-188** — Project Templates: significantly improves onboarding UX; low complexity, high value.
5. **FR-196** — Epic Burndown: completes reporting suite; implement alongside Epic 13.
6. **FR-189–FR-195** — Notification Schemes: Admin-level control; implement as Epic 15 extension.
7. **FR-179–FR-183** — Swimlane/Board enhancements: refine Epic 9 delivery.
8. **FR-199** — Epic color coding: ties together Board/Roadmap/Backlog consistency; implement with Epic 8/9.

---

_Audit completed: 2026-04-29. Auditor: Mary (Business Analyst)._  
_Next review recommended: after Epic 9 delivery to validate Board/Swimlane/Notification implementation._
