# Planning Artifacts Changelog — 2026-04-29

**Project:** project-management  
**Author:** Paige (Technical Writer)  
**Audience:** Developers, Project Managers, Product Owners  
**Purpose:** Record every planning change made on 2026-04-29 so any team member can understand what shifted and why.

---

## Summary

Today's session expanded the project from a **7-epic Phase 1 workforce/Gantt tool** into a **15-epic full Jira-competitive platform**. The primary driver was a strategic decision to build Agile Board + Collaboration + Workflow Engine capabilities — features that would allow internal teams to work entirely in this tool instead of splitting between Jira and Excel.

| Metric | Before (2026-04-28) | After (2026-04-29) |
|---|---|---|
| Total epics | 7 | 15 |
| Total stories | 34 | 74 |
| Functional requirements | FR1–FR54 | FR1–FR160 |
| Planning phases scoped | Phase 1 | Phase 1–4 |
| Architecture decisions | AD-01 to AD-08 | AD-01 to AD-15 |
| PRD user journeys | Journey 1–4 | Journey 1–6 |

---

## File-by-File Changes

### 1. `prd.md` — Version 1.0 → 2.0

**Status:** Updated  
**Lines:** 819

| Section | What changed |
|---|---|
| Executive Summary | Added "Vision mở rộng (Phase 2+)" paragraph. Positions the product as a direct Jira competitor with unique differentiators (Vendor Cost Intelligence, Interactive Gantt, Overload Detection, Capacity Heatmap). |
| Success Metrics | Added Phase 2 success metrics: Agile Board adoption %, Sprint velocity tracking, Issue resolution time. |
| User Journeys | Added **Journey 5** (Developer daily workflow on Agile Board) and **Journey 6** (Sprint Planning end-to-end). |
| Scope | Expanded scope to include Issue Model, Agile Board, Workflow Engine, Custom Fields, Automation. |
| Non-Functional Requirements | Updated to reflect S3 attachment storage, FTS performance targets, and Board rendering performance (200+ cards). |

**Why:** The product vision expanded from "Excel replacement" to "internal Jira alternative". All downstream artifacts follow this direction.

---

### 2. `epics.md` — Version 1.0 → 2.0

**Status:** Updated  
**Lines:** 1,916

#### Epics present before today (Phase 1 — unchanged structure)

| Epic | Name | Stories |
|---|---|---|
| 1 | Authentication + Portfolio/Project Setup + Gantt Interactive | 9 |
| 2 | Workforce (People/Vendor) + Rate Model + Audit Foundation | 5 |
| 3 | TimeEntry & Timesheet + Vendor Import + Status/Lock | 6 |
| 4 | Overload Warning (Standard + Predictive) + Cross-project | 4 |
| 5 | Capacity Planning Suite (Heatmap + 4-week Forecast) | 3 |
| 6 | Cost Tracking & Official Reporting + Export | 4 |
| 7 | Operations Layer (Notifications + Transparency Metrics) | 5 |

Note: Epic 7 received 2 additional stories today (7.4, 7.5 — per-event notification triggers and Notification Center) bringing it from 3 to 5 stories.

#### Epics added today (Phase 2–4)

| Epic | Name | Stories | Phase | Priority |
|---|---|---|---|---|
| 8 | Issue Model Migration + Agile Foundation | 4 | 2 | CRITICAL BLOCKER |
| 9 | Agile Board (Scrum + Kanban) | 9 | 2 | Next |
| 10 | Issue Collaboration (Comments, Attachments, Mentions, Watchers) | 6 | 2 | Next |
| 11 | Configurable Workflow Engine | 4 | 2 | Next |
| 12 | Search, Filters & Bulk Operations | 4 | 3 | Phase 3 |
| 13 | Agile Reports + Roadmap | 6 | 3 | Phase 3 |
| 14 | Custom Fields + Labels/Components/Versions | 6 | 3 | Phase 3 |
| 15 | Automation, Webhooks & Permission Schemes | 5 | 4 | Phase 4 |

**Total new stories added today:** 40 (Epics 8–15 plus 2 from Epic 7)

#### Epic 8 — Special note (Critical Blocker)

Story 8.0 (Issue table migration: `tasks` → `issues` via expand-contract pattern) is a **hard dependency** for all of Epics 9–15. No Phase 2–4 story can start until Story 8.0 is complete and the `tasks` view backward-compatibility is verified.

---

### 3. `architecture.md` — Version 1.0 → 2.0

**Status:** Updated  
**Lines:** 2,032

#### Architecture Decisions added today (AD-09 to AD-15)

| Decision | Summary |
|---|---|
| AD-09 | **Issue Model Migration strategy** — expand-contract pattern: add columns to `issues`, create `tasks` VIEW as alias, backfill, deprecate old table gradually. Rollback script required. |
| AD-10 | **FSM Workflow Engine** — Finite State Machine per project. States + transitions + guards stored in DB (JSONB config). Transition validation server-side; UI shows only valid next states. |
| AD-11 | **JSONB Custom Fields** — `custom_fields JSONB` column on `issues`. Schema defined per Issue Type by admin. Validation at API layer. No retroactive schema changes. |
| AD-12 | **PostgreSQL Full-Text Search** — `tsvector` on `issues(title, description)`. GIN index. `ts_rank` scoring. Target: < 300ms for 100k issues. |
| AD-13 | **S3 File Attachments** — Pre-signed upload URLs (PUT), pre-signed download URLs (GET, 1-hour TTL). Metadata in `attachments` table. Virus scan hook (async). Max file size: 25MB. |
| AD-14 | **Sprint & Board data model** — `sprints` table with FSM (planning → active → completed). `sprint_issues` join table. Board columns driven by workflow states. |
| AD-15 | **Automation Rules Engine** — If-Then model: trigger event → condition check → action execution. Rules stored as JSONB. Async execution via background job queue. |

---

### 4. `fr55-fr160-jira-requirements.md` — New file created today

**Status:** New  
**Lines:** 329  
**FR range:** FR55–FR160 (106 requirements)

This file was created today to document all functional requirements for Phase 2–4 features. It extends `epics.md` (which contains FR1–FR54) and explicitly resolves 5 known conflicts (C1–C5) from Phase 1 design.

#### Requirements grouped by domain

| FR Range | Domain | Count |
|---|---|---|
| FR55–FR65 | Issue Model & Types | 11 |
| FR66–FR78 | Agile Board & Sprint | 13 |
| FR79–FR90 | Issue Collaboration | 12 |
| FR91–FR102 | Workflow Engine | 12 |
| FR103–FR115 | Search & Filters | 13 |
| FR116–FR128 | Agile Reports & Roadmap | 13 |
| FR129–FR140 | Custom Fields & Labels | 12 |
| FR141–FR160 | Automation, Webhooks & Permissions | 20 |

#### Conflicts resolved

| ID | Conflict | Resolution |
|---|---|---|
| C1 | `TaskType` enum was fixed/hardcoded | Replaced by configurable Issue Type catalog per project |
| C2 | Single `status` string on tasks | Replaced by FSM workflow states per project |
| C3 | No parent-child relationship on tasks | Added `parent_id` + 3-level hierarchy (Epic → Story → Sub-task) |
| C4 | `assignee` was 1 person only | Kept 1 assignee + added `watchers` list for collaboration |
| C5 | No story points field | Added `story_points` as first-class field on issues |

---

### 5. `ux-design-specification.md` — Phase 1 unchanged

**Status:** Phase 1 complete, Phase 2 not started  
**Lines:** 866

No changes made today. This file covers Phase 1 UX only (Auth, Gantt, Workforce, TimeEntry, Reports). UX for Phase 2 features (Agile Board, Issue Detail page, Workflow transition buttons) has not yet been designed.

**Action required:** Create Phase 2 UX specification before Epic 9 Sprint Planning begins.

---

### 6. `workflow-status.yaml` — Rewritten

**Status:** Updated to reflect current state  
**What changed:** Replaced the minimal 15-line tracking file with a comprehensive YAML that covers:
- All 10 artifact files with status, version, line count, and notes
- All 15 epics with story counts and phase groupings
- Story count summary by phase (34 / 20 / 15 / 5 = 74 total)
- Explicit documentation gaps list
- Immediate next actions with sequencing constraints

---

### 7. Files not yet created (gaps identified today)

| File | Status | Urgency |
|---|---|---|
| `story-8-0-technical-spec.md` | Not created | CRITICAL — needed before Epic 8 dev starts |
| `jira-completeness-audit.md` | Not created | High — Jira coverage matrix for stakeholders |
| Phase 2 UX specification | Not created | High — needed before Epic 9 Sprint Planning |
| OpenAPI spec (Phase 2 endpoints) | Not created | Medium — needed before frontend integration |
| Performance test plan (Board view) | Not created | Medium — 200+ card rendering target undefined |

---

## New Epics — Brief Description for Developers

### Epic 8: Issue Model Migration (MUST DO FIRST)
Migrate the `tasks` table to a richer `issues` table using the **expand-contract** database pattern. The old `tasks` table becomes a VIEW. All existing Gantt and Reporting queries continue to work unchanged. Adds: `issue_type`, `parent_id`, `story_points`, `custom_fields` (JSONB), `resolution`, `environment` columns.

### Epic 9: Agile Board
Full Scrum + Kanban board. Drag cards to change status. Sprint CRUD (create/start/complete/cancel). Backlog management with drag-drop priority ordering. Sprint planning split-view (backlog left, sprint board right). WIP limits per column. Configurable per project (Scrum mode vs Kanban mode).

### Epic 10: Issue Collaboration
Everything that makes issues feel like Jira tickets. Threaded comments with `@mentions`. File attachments via S3 (upload, preview, download, delete). Watchers list (subscribe/unsubscribe). Issue link types (blocks, relates-to, duplicates). Full activity log showing every change in chronological order.

### Epic 11: Configurable Workflow Engine
Admin-defined workflow per project: states + allowed transitions + who can make each transition. Transition validation (conditions + post-functions). UI shows only the valid next-state buttons on the Issue Detail page. Bundled templates: Default, Scrum, Bug Tracking.

### Epic 12: Search + Filters + Bulk Operations
Full-text search on issue title/description using PostgreSQL `tsvector` + GIN index. Visual filter builder (field + operator + value). Saved/shareable filters. Bulk operations: assign multiple issues, bulk status change, bulk label.

### Epic 13: Agile Reports + Roadmap
Burndown chart (story points or hours remaining per day in sprint). Velocity chart (last 10 sprints). Cumulative Flow Diagram. Sprint Report (completed vs incomplete, scope change log). Epic-level Roadmap timeline with progress bars.

### Epic 14: Custom Fields + Labels + Components + Versions
Admin creates custom field definitions (text / number / date / select / multi-select) per Issue Type. Fields render on Issue Detail. Free-form labels (tag cloud, filterable). Project-level Components with component owners. Versions/Releases with fix-version and affects-version fields. Story Points as first-class numeric field.

### Epic 15: Automation + Webhooks + Permissions
If-Then automation rule builder (trigger → condition → action). Built-in templates (auto-assign on create, auto-transition on resolve, due-date reminder). Outbound webhooks: POST to external URL on issue events (for CI/CD or Slack integrations). Permission schemes: role-based (Admin / Developer / Reporter / Viewer) with a fine-grained permission matrix per project.

---

## Sequencing Constraints Added Today

The following **hard dependencies** were identified and documented in `workflow-status.yaml`:

1. **Story 8.0 must complete before any Epic 9–15 story starts.** The `issues` table and `tasks` VIEW must exist before the Board, Collaboration, and Workflow Engine can build on top of them.

2. **Resource-User identity bridge must complete before Epic 10.** `@mentions` and Watchers require a stable User entity tied to the Resource records created in Epic 2.

3. **UX sign-off on Story 9.1 (Board view) before Sprint 9 starts.** Board UX is complex enough that starting development without a design spec risks expensive rework.

---

## What Has Not Changed

- Phase 1 epic stories (1–7) are **unchanged** in structure and acceptance criteria, except Epic 7 gained 2 new stories (7.4, 7.5).
- The **core differentiators** (Vendor Cost Intelligence, Interactive Gantt, Overload Detection, Capacity Heatmap) remain the primary Phase 1 deliverables and are not deprioritized by the Phase 2 expansion.
- The **tech stack** (Angular SPA + .NET Modular Monolith + PostgreSQL) is unchanged. Phase 2 adds S3 (attachments) and a background job queue (automation/PDF export) but does not alter the core stack.
- **Bryntum Gantt** license requirement is unchanged and remains a prerequisite for Story 1.5.
