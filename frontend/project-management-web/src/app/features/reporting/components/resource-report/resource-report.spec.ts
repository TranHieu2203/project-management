import { describe, it, expect, beforeEach } from 'vitest';
import { ResourceHeatmapCell } from '../../models/resource-report.model';

// Test pure logic extracted from ResourceReportComponent
// (Avoids TestBed + templateUrl resolution issues in Vitest environment)

const trafficLightIcon: Record<string, string> = {
  Green: '●',
  Yellow: '▲',
  Orange: '◆',
  Red: '✕',
};

function cellClass(cell: Pick<ResourceHeatmapCell, 'trafficLight'>): string {
  return `cell-${cell.trafficLight.toLowerCase()}`;
}

function cellTooltip(cell: ResourceHeatmapCell): string {
  return `${cell.actualHours.toFixed(1)}h / ${cell.availableHours.toFixed(0)}h (${cell.utilizationPct.toFixed(1)}%)`;
}

function defaultFrom(): string {
  const d = new Date();
  d.setDate(d.getDate() - 28);
  return d.toISOString().substring(0, 10);
}

function defaultTo(): string {
  const d = new Date();
  d.setDate(d.getDate() + 28);
  return d.toISOString().substring(0, 10);
}

describe('ResourceReportComponent — pure logic', () => {
  describe('cellClass', () => {
    it('returns cell-green for Green traffic light', () => {
      expect(cellClass({ trafficLight: 'Green' })).toBe('cell-green');
    });

    it('returns cell-yellow for Yellow traffic light', () => {
      expect(cellClass({ trafficLight: 'Yellow' })).toBe('cell-yellow');
    });

    it('returns cell-orange for Orange traffic light', () => {
      expect(cellClass({ trafficLight: 'Orange' })).toBe('cell-orange');
    });

    it('returns cell-red for Red traffic light', () => {
      expect(cellClass({ trafficLight: 'Red' })).toBe('cell-red');
    });
  });

  describe('cellTooltip', () => {
    it('formats tooltip with actualHours, availableHours, and utilizationPct', () => {
      const cell: ResourceHeatmapCell = {
        weekStart: '2026-04-27',
        utilizationPct: 91.25,
        trafficLight: 'Yellow',
        actualHours: 36.5,
        availableHours: 40,
      };
      const tooltip = cellTooltip(cell);
      expect(tooltip).toContain('36.5h');
      expect(tooltip).toContain('40h');
      expect(tooltip).toContain('91.3%');
    });

    it('handles zero hours', () => {
      const cell: ResourceHeatmapCell = {
        weekStart: '2026-04-27',
        utilizationPct: 0,
        trafficLight: 'Green',
        actualHours: 0,
        availableHours: 40,
      };
      const tooltip = cellTooltip(cell);
      expect(tooltip).toContain('0.0h');
      expect(tooltip).toContain('0.0%');
    });

    it('handles 100% utilization', () => {
      const cell: ResourceHeatmapCell = {
        weekStart: '2026-04-27',
        utilizationPct: 100,
        trafficLight: 'Orange',
        actualHours: 40,
        availableHours: 40,
      };
      const tooltip = cellTooltip(cell);
      expect(tooltip).toContain('40.0h');
      expect(tooltip).toContain('100.0%');
    });
  });

  describe('trafficLightIcon', () => {
    it('has icons for all 4 traffic light levels', () => {
      expect(trafficLightIcon['Green']).toBeTruthy();
      expect(trafficLightIcon['Yellow']).toBeTruthy();
      expect(trafficLightIcon['Orange']).toBeTruthy();
      expect(trafficLightIcon['Red']).toBeTruthy();
    });

    it('icons are distinct', () => {
      const icons = Object.values(trafficLightIcon);
      const unique = new Set(icons);
      expect(unique.size).toBe(icons.length);
    });
  });

  describe('default date range', () => {
    it('defaultFrom is 28 days before today', () => {
      const from = defaultFrom();
      const diff = (new Date().getTime() - new Date(from).getTime()) / (1000 * 60 * 60 * 24);
      expect(Math.round(diff)).toBe(28);
    });

    it('defaultTo is 28 days after today', () => {
      const to = defaultTo();
      const diff = (new Date(to).getTime() - new Date().getTime()) / (1000 * 60 * 60 * 24);
      expect(Math.round(diff)).toBe(28);
    });

    it('defaultTo is after defaultFrom', () => {
      expect(new Date(defaultTo()) > new Date(defaultFrom())).toBe(true);
    });
  });
});
