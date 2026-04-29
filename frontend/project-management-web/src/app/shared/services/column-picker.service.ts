import { Injectable } from '@angular/core';

export interface ColumnDef {
  id: string;
  label: string;
  defaultVisible: boolean;
  required?: boolean;
}

export interface ColumnPickerConfig {
  componentId: string;
  columns: ColumnDef[];
}

@Injectable({ providedIn: 'root' })
export class ColumnPickerService {
  private readonly state = new Map<string, Set<string>>();

  private storageKey(componentId: string): string {
    return `column-visibility-${componentId}`;
  }

  loadColumns(config: ColumnPickerConfig): void {
    const saved = localStorage.getItem(this.storageKey(config.componentId));
    if (saved) {
      try {
        const parsed: Record<string, boolean> = JSON.parse(saved);
        const visible = new Set(
          config.columns
            .filter(c => c.required || parsed[c.id] !== false)
            .map(c => c.id)
        );
        this.state.set(config.componentId, visible);
        return;
      } catch { /* fall through to defaults */ }
    }
    this.state.set(
      config.componentId,
      new Set(config.columns.filter(c => c.defaultVisible || c.required).map(c => c.id))
    );
  }

  toggleColumn(componentId: string, columnId: string, columns: ColumnDef[]): void {
    const col = columns.find(c => c.id === columnId);
    if (col?.required) return;
    const visible = this.state.get(componentId) ?? new Set<string>();
    const next = new Set(visible);
    next.has(columnId) ? next.delete(columnId) : next.add(columnId);
    this.state.set(componentId, next);
    this.save(componentId, columns);
  }

  getVisibleColumnIds(componentId: string): string[] {
    return [...(this.state.get(componentId) ?? new Set<string>())];
  }

  isVisible(componentId: string, columnId: string): boolean {
    return this.state.get(componentId)?.has(columnId) ?? false;
  }

  getGridTemplate(componentId: string, columns: ColumnDef[], colWidths: Record<string, string>): string {
    const visible = this.state.get(componentId) ?? new Set<string>();
    return columns
      .filter(c => visible.has(c.id))
      .map(c => colWidths[c.id] ?? '1fr')
      .join(' ');
  }

  resetColumns(componentId: string, columns: ColumnDef[]): void {
    this.state.set(
      componentId,
      new Set(columns.filter(c => c.defaultVisible || c.required).map(c => c.id))
    );
    localStorage.removeItem(this.storageKey(componentId));
  }

  private save(componentId: string, columns: ColumnDef[]): void {
    const visible = this.state.get(componentId) ?? new Set<string>();
    const obj: Record<string, boolean> = {};
    columns.forEach(c => { obj[c.id] = visible.has(c.id); });
    try {
      localStorage.setItem(this.storageKey(componentId), JSON.stringify(obj));
    } catch { /* ignore quota errors */ }
  }
}
