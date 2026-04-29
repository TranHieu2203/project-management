import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TaskTreeComponent } from './task-tree';
import { DeadlineAlertService } from '../../services/deadline-alert.service';
import { ProjectTask } from '../../models/task.model';
import { ColumnPickerService } from '../../../../shared/services/column-picker.service';

const TODAY = '2026-04-26';

function makeTask(overrides: Partial<ProjectTask> = {}): ProjectTask {
  return {
    id: 'task-1',
    projectId: 'proj-1',
    parentId: null,
    type: 'Task',
    vbs: null,
    name: 'Test Task',
    priority: 'Medium',
    status: 'InProgress',
    notes: null,
    plannedStartDate: null,
    plannedEndDate: null,
    actualStartDate: null,
    actualEndDate: null,
    plannedEffortHours: null,
    actualEffortHours: null,
    percentComplete: null,
    assigneeUserId: null,
    sortOrder: 1000,
    version: 1,
    predecessors: [],
    ...overrides,
  };
}

const columnPickerMock = {
  loadColumns: vi.fn(),
  isVisible: vi.fn().mockReturnValue(true),
  toggleColumn: vi.fn(),
  resetColumns: vi.fn(),
  getGridTemplate: vi.fn().mockReturnValue('100px'),
  getVisibleColumnIds: vi.fn().mockReturnValue([]),
};

describe('TaskTreeComponent – rowClasses & data-task-id', () => {
  let fixture: ComponentFixture<TaskTreeComponent>;
  let component: TaskTreeComponent;
  let service: DeadlineAlertService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TaskTreeComponent],
      providers: [{ provide: ColumnPickerService, useValue: columnPickerMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(TaskTreeComponent);
    component = fixture.componentInstance;
    service = TestBed.inject(DeadlineAlertService);

    vi.spyOn(service, 'getLocalDateString').mockReturnValue(TODAY);
    component.today = TODAY;
  });

  it('returns row-overdue for a task with plannedEndDate before today', () => {
    const task = makeTask({ plannedEndDate: '2026-04-20' });
    const classes = component.rowClasses(task);
    expect(classes['row-overdue']).toBe(true);
    expect(classes['row-due-today']).toBe(false);
    expect(classes['row-due-soon']).toBe(false);
  });

  it('returns row-due-today for plannedEndDate === today', () => {
    const task = makeTask({ plannedEndDate: TODAY });
    const classes = component.rowClasses(task);
    expect(classes['row-due-today']).toBe(true);
    expect(classes['row-overdue']).toBe(false);
  });

  it('returns row-due-soon for plannedEndDate within 7 days', () => {
    const task = makeTask({ plannedEndDate: '2026-05-03' });
    const classes = component.rowClasses(task);
    expect(classes['row-due-soon']).toBe(true);
    expect(classes['row-overdue']).toBe(false);
  });

  it('returns no deadline class for plannedEndDate beyond 7 days', () => {
    const task = makeTask({ plannedEndDate: '2026-05-10' });
    const classes = component.rowClasses(task);
    expect(classes['row-overdue']).toBe(false);
    expect(classes['row-due-today']).toBe(false);
    expect(classes['row-due-soon']).toBe(false);
  });

  it('excludes Phase from deadline coloring', () => {
    const task = makeTask({ type: 'Phase', plannedEndDate: '2026-04-20' });
    const classes = component.rowClasses(task);
    expect(classes['row-overdue']).toBe(false);
  });

  it('excludes Milestone from deadline coloring', () => {
    const task = makeTask({ type: 'Milestone', plannedEndDate: '2026-04-20' });
    const classes = component.rowClasses(task);
    expect(classes['row-overdue']).toBe(false);
  });

  it('returns no deadline class for Completed tasks', () => {
    const task = makeTask({ status: 'Completed', plannedEndDate: '2026-04-20' });
    const classes = component.rowClasses(task);
    expect(classes['row-overdue']).toBe(false);
  });

  it('sets row-filtered when activeDeadlineFilter matches task status', () => {
    const task = makeTask({ plannedEndDate: '2026-04-20' });
    component.activeDeadlineFilter = 'overdue';
    const classes = component.rowClasses(task);
    expect(classes['row-filtered']).toBe(true);
  });

  it('does not set row-filtered when task status does not match filter', () => {
    const task = makeTask({ plannedEndDate: TODAY });
    component.activeDeadlineFilter = 'overdue';
    const classes = component.rowClasses(task);
    expect(classes['row-filtered']).toBe(false);
  });

  it('sets row-highlight for the highlighted task id', () => {
    const task = makeTask({ id: 'abc' });
    component.highlightTaskId = 'abc';
    const classes = component.rowClasses(task);
    expect(classes['row-highlight']).toBe(true);
  });

  it('renders data-task-id attribute on each task row', () => {
    component.tasks = [makeTask({ id: 'test-id-1' })];
    fixture.detectChanges();
    const row = fixture.debugElement.query(By.css('[data-task-id="test-id-1"]'));
    expect(row).not.toBeNull();
  });
});
