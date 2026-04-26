import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DeadlineAlertBannerComponent } from './deadline-alert-banner';
import { By } from '@angular/platform-browser';

describe('DeadlineAlertBannerComponent', () => {
  let fixture: ComponentFixture<DeadlineAlertBannerComponent>;
  let component: DeadlineAlertBannerComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DeadlineAlertBannerComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(DeadlineAlertBannerComponent);
    component = fixture.componentInstance;
  });

  it('renders no badge-overdue button when overdueCnt = 0', () => {
    component.overdueCnt = 0;
    fixture.detectChanges();
    const badge = fixture.debugElement.query(By.css('.badge-overdue'));
    expect(badge).toBeNull();
  });

  it('renders badge-overdue when overdueCnt > 0', () => {
    component.overdueCnt = 3;
    fixture.detectChanges();
    const badge = fixture.debugElement.query(By.css('.badge-overdue'));
    expect(badge).not.toBeNull();
    expect(badge.nativeElement.textContent).toContain('3');
  });

  it('renders badge-today when dueTodayCnt > 0', () => {
    component.dueTodayCnt = 1;
    fixture.detectChanges();
    const badge = fixture.debugElement.query(By.css('.badge-today'));
    expect(badge).not.toBeNull();
    expect(badge.nativeElement.textContent).toContain('1');
  });

  it('renders badge-soon when dueSoonCnt > 0', () => {
    component.dueSoonCnt = 5;
    fixture.detectChanges();
    const badge = fixture.debugElement.query(By.css('.badge-soon'));
    expect(badge).not.toBeNull();
  });

  it('emits filterChange with group on badge click', () => {
    component.overdueCnt = 2;
    fixture.detectChanges();
    const emitted: any[] = [];
    component.filterChange.subscribe(v => emitted.push(v));
    fixture.debugElement.query(By.css('.badge-overdue')).nativeElement.click();
    expect(emitted).toEqual(['overdue']);
  });

  it('emits filterChange with null (toggle off) when clicking already-active badge', () => {
    component.overdueCnt = 2;
    component.activeFilter = 'overdue';
    fixture.detectChanges();
    const emitted: any[] = [];
    component.filterChange.subscribe(v => emitted.push(v));
    fixture.debugElement.query(By.css('.badge-overdue')).nativeElement.click();
    expect(emitted).toEqual([null]);
  });

  it('adds badge-active class to active group badge', () => {
    component.overdueCnt = 2;
    component.activeFilter = 'overdue';
    fixture.detectChanges();
    const badge = fixture.debugElement.query(By.css('.badge-overdue'));
    expect(badge.nativeElement.classList).toContain('badge-active');
  });

  it('shows clear-filter-btn when activeFilter is set', () => {
    component.overdueCnt = 1;
    component.activeFilter = 'overdue';
    fixture.detectChanges();
    const btn = fixture.debugElement.query(By.css('.clear-filter-btn'));
    expect(btn).not.toBeNull();
  });

  it('hides clear-filter-btn when activeFilter is null', () => {
    component.activeFilter = null;
    fixture.detectChanges();
    const btn = fixture.debugElement.query(By.css('.clear-filter-btn'));
    expect(btn).toBeNull();
  });
});
