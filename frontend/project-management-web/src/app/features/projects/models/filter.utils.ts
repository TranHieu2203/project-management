import { Params } from '@angular/router';
import { ProjectTask } from './task.model';
import { FilterCriteria, TaskNodeType, TaskPriority, TaskStatus } from './filter.model';

/** Trả true nếu không có criterion nào active */
export function isEmpty(criteria: FilterCriteria | null | undefined): boolean {
  if (!criteria) return true;
  return (
    !criteria.keyword &&
    !criteria.statuses?.length &&
    !criteria.assigneeIds?.length &&
    !criteria.priorities?.length &&
    !criteria.nodeTypes?.length &&
    !criteria.milestoneId &&
    !criteria.dueDateFrom &&
    !criteria.dueDateTo &&
    !criteria.overdueOnly
  );
}

/** Deep equality giữa 2 FilterCriteria */
export function criteriaEquals(a: FilterCriteria, b: FilterCriteria): boolean {
  return JSON.stringify(serializeFilter(a)) === JSON.stringify(serializeFilter(b));
}

/**
 * Áp dụng filter trên flat list tasks.
 * Trả về ids của tasks MATCH (không bao gồm ancestor context).
 */
export function applyFilter(
  tasks: ProjectTask[],
  criteria: FilterCriteria,
  currentUserId: string,
  today: string
): string[] {
  if (isEmpty(criteria)) return tasks.map(t => t.id);

  const matchingIds = new Set<string>();

  for (const task of tasks) {
    if (matchesTask(task, criteria, currentUserId, today)) {
      matchingIds.add(task.id);
    }
  }

  return Array.from(matchingIds);
}

/**
 * Tính toán set ids cần hiển thị, bao gồm cả ancestor context nodes.
 * Trả về Map<id, isMatch> — false = ancestor context, true = match.
 */
export function computeVisibleIds(
  tasks: ProjectTask[],
  criteria: FilterCriteria,
  currentUserId: string,
  today: string
): Map<string, boolean> {
  if (isEmpty(criteria)) {
    const m = new Map<string, boolean>();
    tasks.forEach(t => m.set(t.id, true));
    return m;
  }

  // Bước 1: tìm tasks match
  const matchingIds = new Set<string>();
  for (const task of tasks) {
    if (matchesTask(task, criteria, currentUserId, today)) {
      matchingIds.add(task.id);
    }
  }

  // Bước 2: thu thập ancestors của tasks match
  const taskMap = new Map(tasks.map(t => [t.id, t]));
  const visibleMap = new Map<string, boolean>();

  for (const id of matchingIds) {
    visibleMap.set(id, true);
    // Walk up ancestor chain
    let current = taskMap.get(id);
    while (current?.parentId) {
      if (!visibleMap.has(current.parentId)) {
        visibleMap.set(current.parentId, false); // ancestor context
      }
      current = taskMap.get(current.parentId);
    }
  }

  return visibleMap;
}

function matchesTask(
  task: ProjectTask,
  criteria: FilterCriteria,
  currentUserId: string,
  today: string
): boolean {
  // Keyword
  if (criteria.keyword) {
    const kw = criteria.keyword.toLowerCase();
    const nameMatch = task.name.toLowerCase().includes(kw);
    const vbsMatch = task.vbs?.toLowerCase().includes(kw) ?? false;
    if (!nameMatch && !vbsMatch) return false;
  }

  // Status
  if (criteria.statuses?.length) {
    if (!criteria.statuses.includes(task.status as TaskStatus)) return false;
  }

  // Assignee
  if (criteria.assigneeIds?.length) {
    const hasCurrentUser = criteria.assigneeIds.includes('CURRENT_USER');
    const hasUnassigned = criteria.assigneeIds.includes('UNASSIGNED');
    const otherIds = criteria.assigneeIds.filter(
      id => id !== 'CURRENT_USER' && id !== 'UNASSIGNED'
    );

    const taskAssigneeId = task.assigneeUserId;
    let matches = false;
    if (hasCurrentUser && taskAssigneeId === currentUserId) matches = true;
    if (hasUnassigned && !taskAssigneeId) matches = true;
    if (otherIds.length && taskAssigneeId && otherIds.includes(taskAssigneeId)) matches = true;
    if (!matches) return false;
  }

  // Priority
  if (criteria.priorities?.length) {
    if (!criteria.priorities.includes(task.priority as TaskPriority)) return false;
  }

  // Node type
  if (criteria.nodeTypes?.length) {
    if (!criteria.nodeTypes.includes(task.type as TaskNodeType)) return false;
  }

  // Due date range (check plannedEndDate)
  if (criteria.dueDateFrom && task.plannedEndDate) {
    if (task.plannedEndDate < criteria.dueDateFrom) return false;
  }
  if (criteria.dueDateTo && task.plannedEndDate) {
    if (task.plannedEndDate > criteria.dueDateTo) return false;
  }

  // Overdue only
  if (criteria.overdueOnly) {
    const isOverdue =
      !!task.plannedEndDate &&
      task.plannedEndDate < today &&
      task.status !== 'Completed' &&
      task.status !== 'Cancelled';
    if (!isOverdue) return false;
  }

  return true;
}

/** Chuyển FilterCriteria → Angular Router Params (cho URL sync) */
export function serializeFilter(criteria: FilterCriteria): Params {
  const params: Params = {};
  if (criteria.keyword) params['q'] = criteria.keyword;
  if (criteria.statuses?.length) params['status'] = criteria.statuses.join(',');
  if (criteria.assigneeIds?.length) params['assignee'] = criteria.assigneeIds.join(',');
  if (criteria.priorities?.length) params['priority'] = criteria.priorities.join(',');
  if (criteria.nodeTypes?.length) params['type'] = criteria.nodeTypes.join(',');
  if (criteria.milestoneId) params['milestone'] = criteria.milestoneId;
  if (criteria.dueDateFrom) params['dateFrom'] = criteria.dueDateFrom;
  if (criteria.dueDateTo) params['dateTo'] = criteria.dueDateTo;
  if (criteria.overdueOnly) params['overdue'] = 'true';
  return params;
}

/** Chuyển Angular Router queryParams → FilterCriteria */
export function parseQueryParams(params: Params): FilterCriteria {
  const criteria: FilterCriteria = {};
  if (params['q']) criteria.keyword = params['q'];
  if (params['status']) criteria.statuses = params['status'].split(',') as TaskStatus[];
  if (params['assignee']) criteria.assigneeIds = params['assignee'].split(',');
  if (params['priority']) criteria.priorities = params['priority'].split(',') as TaskPriority[];
  if (params['type']) criteria.nodeTypes = params['type'].split(',') as TaskNodeType[];
  if (params['milestone']) criteria.milestoneId = params['milestone'];
  if (params['dateFrom']) criteria.dueDateFrom = params['dateFrom'];
  if (params['dateTo']) criteria.dueDateTo = params['dateTo'];
  if (params['overdue'] === 'true') criteria.overdueOnly = true;
  return criteria;
}

/** Đếm số criterion đang active */
export function countActiveFilters(criteria: FilterCriteria | null | undefined): number {
  if (!criteria) return 0;
  let count = 0;
  if (criteria.keyword) count++;
  if (criteria.statuses?.length) count++;
  if (criteria.assigneeIds?.length) count++;
  if (criteria.priorities?.length) count++;
  if (criteria.nodeTypes?.length) count++;
  if (criteria.milestoneId) count++;
  if (criteria.dueDateFrom || criteria.dueDateTo) count++;
  if (criteria.overdueOnly) count++;
  return count;
}

/** Xóa một criterion cụ thể */
export function removeCriterion(
  criteria: FilterCriteria,
  key: keyof FilterCriteria
): FilterCriteria {
  const next = { ...criteria };
  delete next[key];
  return next;
}

/** Format label cho active filter chip */
export function getChipLabel(key: keyof FilterCriteria, criteria: FilterCriteria): string {
  switch (key) {
    case 'keyword': return `Từ khóa: "${criteria.keyword}"`;
    case 'statuses': return `Trạng thái: ${criteria.statuses?.join(', ')}`;
    case 'assigneeIds': return `Người thực hiện`;
    case 'priorities': return `Ưu tiên: ${criteria.priorities?.join(', ')}`;
    case 'nodeTypes': return `Loại: ${criteria.nodeTypes?.join(', ')}`;
    case 'milestoneId': return `Milestone`;
    case 'dueDateFrom':
    case 'dueDateTo': return `Ngày kết thúc`;
    case 'overdueOnly': return `Quá hạn`;
    default: return key;
  }
}
