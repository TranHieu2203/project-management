import { Injectable } from '@angular/core';
import { ProjectTask } from '../../projects/models/task.model';
import { GanttTask, GanttTaskType } from '../models/gantt.model';

@Injectable({ providedIn: 'root' })
export class GanttAdapterService {

  adapt(tasks: ProjectTask[]): GanttTask[] {
    const depthMap = this.buildDepthMap(tasks);

    return [...tasks]
      .sort((a, b) => a.sortOrder - b.sortOrder)
      .map(t => ({
        id: t.id,
        parentId: t.parentId,
        type: t.type as GanttTaskType,
        vbs: t.vbs ?? null,
        name: t.name,
        status: t.status,
        priority: t.priority,
        plannedStart: t.plannedStartDate ? this.parseDate(t.plannedStartDate) : null,
        plannedEnd: t.plannedEndDate ? this.parseDate(t.plannedEndDate) : null,
        percentComplete: t.percentComplete ?? 0,
        depth: depthMap.get(t.id) ?? 0,
        sortOrder: t.sortOrder,
        collapsed: false,
        version: t.version,
        dirty: false,
        assigneeUserId: t.assigneeUserId ?? null,
      }));
  }

  private buildDepthMap(tasks: ProjectTask[]): Map<string, number> {
    const parentMap = new Map(tasks.map(t => [t.id, t.parentId]));
    const depthMap = new Map<string, number>();

    const getDepth = (id: string): number => {
      if (depthMap.has(id)) return depthMap.get(id)!;
      const parentId = parentMap.get(id);
      const depth = parentId ? getDepth(parentId) + 1 : 0;
      depthMap.set(id, depth);
      return depth;
    };

    tasks.forEach(t => getDepth(t.id));
    return depthMap;
  }

  // Parse "YYYY-MM-DD" DateOnly string to Date
  private parseDate(dateStr: string): Date {
    const [year, month, day] = dateStr.split('-').map(Number);
    return new Date(year, month - 1, day);
  }
}
