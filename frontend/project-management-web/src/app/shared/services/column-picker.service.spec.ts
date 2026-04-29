import { TestBed } from '@angular/core/testing';
import { ColumnDef, ColumnPickerConfig, ColumnPickerService } from './column-picker.service';

const COLUMNS: ColumnDef[] = [
  { id: 'name',   label: 'Tên',    defaultVisible: true,  required: true },
  { id: 'status', label: 'Trạng thái', defaultVisible: true  },
  { id: 'type',   label: 'Loại',   defaultVisible: false },
];

const CONFIG: ColumnPickerConfig = { componentId: 'test-comp', columns: COLUMNS };

describe('ColumnPickerService', () => {
  let service: ColumnPickerService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(ColumnPickerService);
  });

  afterEach(() => localStorage.clear());

  describe('loadColumns', () => {
    it('loads defaults when no localStorage entry exists', () => {
      service.loadColumns(CONFIG);
      expect(service.isVisible('test-comp', 'name')).toBe(true);
      expect(service.isVisible('test-comp', 'status')).toBe(true);
      expect(service.isVisible('test-comp', 'type')).toBe(false);
    });

    it('restores saved state from localStorage', () => {
      localStorage.setItem('column-visibility-test-comp', JSON.stringify({ name: true, status: false, type: true }));
      service.loadColumns(CONFIG);
      // name is required → always visible even if saved as false
      expect(service.isVisible('test-comp', 'name')).toBe(true);
      expect(service.isVisible('test-comp', 'status')).toBe(false);
      expect(service.isVisible('test-comp', 'type')).toBe(true);
    });

    it('falls back to defaults when localStorage has corrupted JSON', () => {
      localStorage.setItem('column-visibility-test-comp', 'NOT_JSON{{{');
      service.loadColumns(CONFIG);
      expect(service.isVisible('test-comp', 'name')).toBe(true);
      expect(service.isVisible('test-comp', 'status')).toBe(true);
      expect(service.isVisible('test-comp', 'type')).toBe(false);
    });

    it('required column is always visible even if saved as hidden', () => {
      localStorage.setItem('column-visibility-test-comp', JSON.stringify({ name: false, status: true, type: false }));
      service.loadColumns(CONFIG);
      expect(service.isVisible('test-comp', 'name')).toBe(true);
    });
  });

  describe('toggleColumn', () => {
    beforeEach(() => service.loadColumns(CONFIG));

    it('toggles a non-required column off and back on', () => {
      expect(service.isVisible('test-comp', 'status')).toBe(true);
      service.toggleColumn('test-comp', 'status', COLUMNS);
      expect(service.isVisible('test-comp', 'status')).toBe(false);
      service.toggleColumn('test-comp', 'status', COLUMNS);
      expect(service.isVisible('test-comp', 'status')).toBe(true);
    });

    it('does NOT toggle required column', () => {
      service.toggleColumn('test-comp', 'name', COLUMNS);
      expect(service.isVisible('test-comp', 'name')).toBe(true);
    });

    it('persists change to localStorage', () => {
      service.toggleColumn('test-comp', 'status', COLUMNS);
      const saved = JSON.parse(localStorage.getItem('column-visibility-test-comp')!);
      expect(saved['status']).toBe(false);
    });
  });

  describe('getVisibleColumnIds', () => {
    it('returns only visible column ids', () => {
      service.loadColumns(CONFIG);
      const ids = service.getVisibleColumnIds('test-comp');
      expect(ids).toContain('name');
      expect(ids).toContain('status');
      expect(ids).not.toContain('type');
    });
  });

  describe('isVisible', () => {
    it('returns true for visible column', () => {
      service.loadColumns(CONFIG);
      expect(service.isVisible('test-comp', 'status')).toBe(true);
    });

    it('returns false for hidden column', () => {
      service.loadColumns(CONFIG);
      expect(service.isVisible('test-comp', 'type')).toBe(false);
    });

    it('returns false for unknown componentId', () => {
      expect(service.isVisible('unknown', 'name')).toBe(false);
    });
  });

  describe('getGridTemplate', () => {
    it('returns grid template string for visible columns only', () => {
      service.loadColumns(CONFIG);
      const widths = { name: '1fr', status: '100px', type: '60px' };
      const template = service.getGridTemplate('test-comp', COLUMNS, widths);
      expect(template).toBe('1fr 100px');
    });

    it('uses 1fr fallback for columns without a defined width', () => {
      service.loadColumns(CONFIG);
      const template = service.getGridTemplate('test-comp', COLUMNS, {});
      expect(template).toBe('1fr 1fr');
    });
  });

  describe('resetColumns', () => {
    it('restores default visibility and removes localStorage entry', () => {
      service.loadColumns(CONFIG);
      service.toggleColumn('test-comp', 'status', COLUMNS);
      service.toggleColumn('test-comp', 'type', COLUMNS);

      service.resetColumns('test-comp', COLUMNS);

      expect(service.isVisible('test-comp', 'name')).toBe(true);
      expect(service.isVisible('test-comp', 'status')).toBe(true);
      expect(service.isVisible('test-comp', 'type')).toBe(false);
      expect(localStorage.getItem('column-visibility-test-comp')).toBeNull();
    });
  });
});
