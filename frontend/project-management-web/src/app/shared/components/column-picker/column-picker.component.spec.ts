import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ColumnPickerComponent } from './column-picker.component';
import { ColumnDef, ColumnPickerService } from '../../services/column-picker.service';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

const COLUMNS: ColumnDef[] = [
  { id: 'name',   label: 'Tên',         defaultVisible: true,  required: true },
  { id: 'status', label: 'Trạng thái',  defaultVisible: true },
  { id: 'type',   label: 'Loại',        defaultVisible: false },
];

describe('ColumnPickerComponent', () => {
  let fixture: ComponentFixture<ColumnPickerComponent>;
  let component: ColumnPickerComponent;
  let service: ColumnPickerService;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [ColumnPickerComponent, NoopAnimationsModule],
    }).compileComponents();

    fixture = TestBed.createComponent(ColumnPickerComponent);
    component = fixture.componentInstance;
    service = TestBed.inject(ColumnPickerService);

    component.componentId = 'test-comp';
    component.columns = COLUMNS;
    service.loadColumns({ componentId: 'test-comp', columns: COLUMNS });
    fixture.detectChanges();
  });

  afterEach(() => localStorage.clear());

  it('renders checkbox for each column', () => {
    const checkboxes = fixture.nativeElement.querySelectorAll('input[type="checkbox"]');
    expect(checkboxes.length).toBe(COLUMNS.length);
  });

  it('renders required column checkbox as disabled', () => {
    const checkboxes: NodeListOf<HTMLInputElement> = fixture.nativeElement.querySelectorAll('input[type="checkbox"]');
    expect(checkboxes[0].disabled).toBe(true);
  });

  it('non-required column checkbox is not disabled', () => {
    const checkboxes: NodeListOf<HTMLInputElement> = fixture.nativeElement.querySelectorAll('input[type="checkbox"]');
    expect(checkboxes[1].disabled).toBe(false);
  });

  it('toggle emits changed event', () => {
    let emitted = false;
    component.changed.subscribe(() => { emitted = true; });
    component.toggle('status');
    expect(emitted).toBe(true);
  });

  it('reset emits changed event', () => {
    let emitted = false;
    component.changed.subscribe(() => { emitted = true; });
    component.reset();
    expect(emitted).toBe(true);
  });

  it('toggle calls service.toggleColumn', () => {
    const spy = vi.spyOn(service, 'toggleColumn');
    component.toggle('status');
    expect(spy).toHaveBeenCalledWith('test-comp', 'status', COLUMNS);
  });

  it('reset calls service.resetColumns', () => {
    const spy = vi.spyOn(service, 'resetColumns');
    component.reset();
    expect(spy).toHaveBeenCalledWith('test-comp', COLUMNS);
  });

  it('isVisible delegates to service', () => {
    expect(component.isVisible('status')).toBe(service.isVisible('test-comp', 'status'));
  });
});
