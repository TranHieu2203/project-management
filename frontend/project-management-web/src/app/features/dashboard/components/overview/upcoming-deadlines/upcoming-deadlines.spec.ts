import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { vi } from 'vitest';
import { UpcomingDeadlinesComponent } from './upcoming-deadlines';
import { Deadline } from '../../../models/dashboard.model';

describe('UpcomingDeadlinesComponent', () => {
  let fixture: ComponentFixture<UpcomingDeadlinesComponent>;
  let component: UpcomingDeadlinesComponent;

  const makeDeadline = (overrides: Partial<Deadline> = {}): Deadline => ({
    taskId: 'task-1',
    projectId: 'proj-1',
    projectName: 'Alpha Project',
    entityType: 'Task',
    name: 'Design mockups',
    dueDate: '2026-05-01',
    daysRemaining: 3,
    ...overrides,
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UpcomingDeadlinesComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(UpcomingDeadlinesComponent);
    component = fixture.componentInstance;
  });

  // ─── Loading skeleton ────────────────────────────────────────────────────────

  it('renders skeleton rows when loading is true', () => {
    component.loading = true;
    fixture.detectChanges();
    const skeletons = fixture.debugElement.queryAll(By.css('.skeleton-item'));
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('does not render deadline list when loading is true', () => {
    component.loading = true;
    fixture.detectChanges();
    const list = fixture.debugElement.query(By.css('.deadline-list'));
    expect(list).toBeNull();
  });

  // ─── Error state ─────────────────────────────────────────────────────────────

  it('renders error state when error is set', () => {
    component.error = 'Không tải được deadlines.';
    fixture.detectChanges();
    const errEl = fixture.debugElement.query(By.css('.widget-error'));
    expect(errEl).not.toBeNull();
    expect(errEl.nativeElement.textContent).toContain('Không tải được deadlines.');
  });

  // ─── Empty state ─────────────────────────────────────────────────────────────

  it('renders empty state when deadlines is empty', () => {
    component.deadlines = [];
    fixture.detectChanges();
    const emptyEl = fixture.debugElement.query(By.css('.empty-state'));
    expect(emptyEl).not.toBeNull();
    expect(emptyEl.nativeElement.textContent).toContain('Không có deadline nào trong 7 ngày tới');
  });

  it('does not render deadline list when deadlines is empty', () => {
    component.deadlines = [];
    fixture.detectChanges();
    const list = fixture.debugElement.query(By.css('.deadline-list'));
    expect(list).toBeNull();
  });

  // ─── Deadline list ───────────────────────────────────────────────────────────

  it('renders one list item per deadline', () => {
    component.deadlines = [makeDeadline(), makeDeadline({ taskId: 'task-2', name: 'Review' })];
    fixture.detectChanges();
    const items = fixture.debugElement.queryAll(By.css('.deadline-item'));
    expect(items.length).toBe(2);
  });

  it('shows task name and project name in each item', () => {
    component.deadlines = [makeDeadline({ name: 'Write tests', projectName: 'Beta Corp' })];
    fixture.detectChanges();
    const item = fixture.debugElement.query(By.css('.deadline-item'));
    expect(item.nativeElement.textContent).toContain('Write tests');
    expect(item.nativeElement.textContent).toContain('Beta Corp');
  });

  it('emits deadlineClick with the correct deadline on item click', () => {
    const dl = makeDeadline({ taskId: 'task-abc' });
    component.deadlines = [dl];
    fixture.detectChanges();
    const spy = vi.spyOn(component.deadlineClick, 'emit');
    fixture.debugElement.query(By.css('.deadline-item')).triggerEventHandler('click', null);
    expect(spy).toHaveBeenCalledOnce();
    expect(spy).toHaveBeenCalledWith(dl);
  });

  // ─── formatDaysRemaining ─────────────────────────────────────────────────────

  it('formatDaysRemaining(0) returns "Hôm nay"', () => {
    expect(component.formatDaysRemaining(0)).toBe('Hôm nay');
  });

  it('formatDaysRemaining(1) returns "Còn 1 ngày"', () => {
    expect(component.formatDaysRemaining(1)).toBe('Còn 1 ngày');
  });

  it('formatDaysRemaining(5) returns "Còn 5 ngày"', () => {
    expect(component.formatDaysRemaining(5)).toBe('Còn 5 ngày');
  });

  // ─── urgencyClass ─────────────────────────────────────────────────────────────

  it('urgencyClass(0) returns "urgent"', () => {
    expect(component.urgencyClass(0)).toBe('urgent');
  });

  it('urgencyClass(1) returns "warning"', () => {
    expect(component.urgencyClass(1)).toBe('warning');
  });

  it('urgencyClass(2) returns "warning"', () => {
    expect(component.urgencyClass(2)).toBe('warning');
  });

  it('urgencyClass(3) returns "normal"', () => {
    expect(component.urgencyClass(3)).toBe('normal');
  });

  it('urgencyClass(7) returns "normal"', () => {
    expect(component.urgencyClass(7)).toBe('normal');
  });

  // ─── Urgency classes applied to items ────────────────────────────────────────

  it('applies "urgent" class to item when daysRemaining is 0', () => {
    component.deadlines = [makeDeadline({ daysRemaining: 0 })];
    fixture.detectChanges();
    const item = fixture.debugElement.query(By.css('.deadline-item'));
    expect(item.nativeElement.classList).toContain('urgent');
  });

  it('applies "warning" class to item when daysRemaining is 2', () => {
    component.deadlines = [makeDeadline({ daysRemaining: 2 })];
    fixture.detectChanges();
    const item = fixture.debugElement.query(By.css('.deadline-item'));
    expect(item.nativeElement.classList).toContain('warning');
  });

  it('applies "normal" class to item when daysRemaining is 5', () => {
    component.deadlines = [makeDeadline({ daysRemaining: 5 })];
    fixture.detectChanges();
    const item = fixture.debugElement.query(By.css('.deadline-item'));
    expect(item.nativeElement.classList).toContain('normal');
  });
});
