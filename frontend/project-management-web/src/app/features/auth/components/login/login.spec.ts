import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { vi } from 'vitest';
import { AuthActions } from '../../store/auth.actions';
import { selectAuthError, selectAuthLoading } from '../../store/auth.selectors';
import { LoginComponent } from './login';

const initialState = { auth: { user: null, isLoading: false, error: null } };

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let store: MockStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent, NoopAnimationsModule],
      providers: [
        provideMockStore({
          initialState,
          selectors: [
            { selector: selectAuthLoading, value: false },
            { selector: selectAuthError, value: null },
          ],
        }),
      ],
    }).compileComponents();

    store = TestBed.inject(MockStore);
    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render email and password fields', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('input[type="email"]')).toBeTruthy();
    expect(el.querySelector('input[type="password"]')).toBeTruthy();
  });

  it('should disable submit button when form is invalid', () => {
    const btn = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(btn.disabled).toBe(true);
  });

  it('should dispatch AuthActions.login on valid submit', () => {
    const dispatchSpy = vi.spyOn(store, 'dispatch');

    const form = (component as any).form;
    form.setValue({ email: 'pm1@local.test', password: 'P@ssw0rd!123' });
    fixture.detectChanges();

    (component as any).onSubmit();

    expect(dispatchSpy).toHaveBeenCalledWith(
      AuthActions.login({ email: 'pm1@local.test', password: 'P@ssw0rd!123' })
    );
  });

  it('should NOT dispatch when form is invalid', () => {
    const dispatchSpy = vi.spyOn(store, 'dispatch');
    (component as any).onSubmit();
    expect(dispatchSpy).not.toHaveBeenCalled();
  });

  it('should display error message when error$ emits', async () => {
    store.overrideSelector(selectAuthError, 'Tên đăng nhập hoặc mật khẩu không đúng');
    store.refreshState();
    fixture.detectChanges();
    await fixture.whenStable();

    const errorEl = fixture.nativeElement.querySelector('.error-message') as HTMLElement;
    expect(errorEl).toBeTruthy();
    expect(errorEl.textContent).toContain('Tên đăng nhập hoặc mật khẩu không đúng');
  });

  it('should disable submit and show spinner when isLoading$ is true', async () => {
    store.overrideSelector(selectAuthLoading, true);
    store.overrideSelector(selectAuthError, null);
    store.refreshState();

    const form = (component as any).form;
    form.setValue({ email: 'pm1@local.test', password: 'P@ssw0rd!123' });
    fixture.detectChanges();
    await fixture.whenStable();

    const btn = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(btn.disabled).toBe(true);

    const spinner = fixture.nativeElement.querySelector('mat-spinner');
    expect(spinner).toBeTruthy();
  });
});
