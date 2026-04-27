import { Injectable } from '@angular/core';
import {
  FilterPreset,
  FILTER_PRESETS_LS_KEY,
  SYSTEM_PRESETS,
} from '../models/filter.model';

@Injectable({ providedIn: 'root' })
export class FilterPresetsService {
  /** Lấy tất cả presets: system defaults + user-defined */
  getAll(): FilterPreset[] {
    return [...SYSTEM_PRESETS, ...this.loadUserPresets()];
  }

  /** Lưu preset mới */
  save(preset: FilterPreset): void {
    const presets = this.loadUserPresets();
    const idx = presets.findIndex(p => p.id === preset.id);
    if (idx >= 0) {
      presets[idx] = preset;
    } else {
      presets.push(preset);
    }
    this.persist(presets);
  }

  /** Xóa user-defined preset (system presets không xóa được) */
  delete(id: string): void {
    const presets = this.loadUserPresets().filter(p => p.id !== id);
    this.persist(presets);
  }

  private loadUserPresets(): FilterPreset[] {
    try {
      const raw = localStorage.getItem(FILTER_PRESETS_LS_KEY);
      return raw ? JSON.parse(raw) : [];
    } catch {
      return [];
    }
  }

  private persist(presets: FilterPreset[]): void {
    try {
      localStorage.setItem(FILTER_PRESETS_LS_KEY, JSON.stringify(presets));
    } catch { /* ignore quota errors */ }
  }

  generateId(): string {
    return `user-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
  }
}
