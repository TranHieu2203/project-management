import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { vi } from 'vitest';
import { StatCardsComponent } from './stat-cards';
import { StatCards } from '../../../models/dashboard.model';

describe('StatCardsComponent', () => {
  let fixture: ComponentFixture<StatCardsComponent>;
  let component: StatCardsComponent;

  const mockData: StatCards = {
    overdueTaskCount: 5,
    atRiskProjectCount: 2,
    overloadedResourceCount: 0,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StatCardsComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(StatCardsComponent);
    component = fixture.componentInstance;
  });

  // ─── Loading skeleton ────────────────────────────────────────────────────────

  it('renders skeleton cards when loading is true', () => {
    component.loading = true;
    fixture.detectChanges();
    const skeletons = fixture.debugElement.queryAll(By.css('.stat-card.skeleton'));
    expect(skeletons.length).toBe(3);
  });

  it('does not render real cards when loading is true', () => {
    component.loading = true;
    fixture.detectChanges();
    const cards = fixture.debugElement.queryAll(By.css('button.stat-card'));
    expect(cards.length).toBe(0);
  });

  // ─── Error state ─────────────────────────────────────────────────────────────

  it('renders error state when error is set', () => {
    component.error = 'Không thể tải stat cards.';
    fixture.detectChanges();
    const errEl = fixture.debugElement.query(By.css('.stat-cards-error'));
    expect(errEl).not.toBeNull();
    expect(errEl.nativeElement.textContent).toContain('Không thể tải stat cards.');
  });

  it('does not render real cards when error is set', () => {
    component.error = 'Some error';
    fixture.detectChanges();
    const cards = fixture.debugElement.queryAll(By.css('button.stat-card'));
    expect(cards.length).toBe(0);
  });

  // ─── Normal state — 3 cards ──────────────────────────────────────────────────

  it('renders 3 stat card buttons when statCards is provided', () => {
    component.statCards = mockData;
    fixture.detectChanges();
    const cards = fixture.debugElement.queryAll(By.css('button.stat-card'));
    expect(cards.length).toBe(3);
  });

  it('displays overdueTaskCount on the overdue card', () => {
    component.statCards = mockData;
    fixture.detectChanges();
    const overdueCard = fixture.debugElement.query(By.css('.stat-card.overdue'));
    expect(overdueCard).not.toBeNull();
    expect(overdueCard.nativeElement.textContent).toContain('5');
  });

  it('displays atRiskProjectCount on the at-risk card', () => {
    component.statCards = mockData;
    fixture.detectChanges();
    const atRiskCard = fixture.debugElement.query(By.css('.stat-card.at-risk'));
    expect(atRiskCard).not.toBeNull();
    expect(atRiskCard.nativeElement.textContent).toContain('2');
  });

  it('displays overloadedResourceCount on the overloaded card', () => {
    component.statCards = { ...mockData, overloadedResourceCount: 3 };
    fixture.detectChanges();
    const overloadedCard = fixture.debugElement.query(By.css('.stat-card.overloaded'));
    expect(overloadedCard).not.toBeNull();
    expect(overloadedCard.nativeElement.textContent).toContain('3');
  });

  it('renders nothing when statCards is null and loading/error are false', () => {
    component.statCards = null;
    fixture.detectChanges();
    const row = fixture.debugElement.query(By.css('.stat-cards-row'));
    expect(row).toBeNull();
  });

  // ─── Click events ─────────────────────────────────────────────────────────────

  it('emits overdueClick when overdue card is clicked', () => {
    component.statCards = mockData;
    fixture.detectChanges();
    const spy = vi.spyOn(component.overdueClick, 'emit');
    fixture.debugElement.query(By.css('.stat-card.overdue')).triggerEventHandler('click', null);
    expect(spy).toHaveBeenCalledOnce();
  });

  it('emits atRiskClick when at-risk card is clicked', () => {
    component.statCards = mockData;
    fixture.detectChanges();
    const spy = vi.spyOn(component.atRiskClick, 'emit');
    fixture.debugElement.query(By.css('.stat-card.at-risk')).triggerEventHandler('click', null);
    expect(spy).toHaveBeenCalledOnce();
  });

  it('emits overloadedClick when overloaded card is clicked', () => {
    component.statCards = mockData;
    fixture.detectChanges();
    const spy = vi.spyOn(component.overloadedClick, 'emit');
    fixture.debugElement.query(By.css('.stat-card.overloaded')).triggerEventHandler('click', null);
    expect(spy).toHaveBeenCalledOnce();
  });
});
