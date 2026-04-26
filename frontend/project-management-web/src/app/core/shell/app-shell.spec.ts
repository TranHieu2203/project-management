import { AppShellComponent } from './app-shell';

const SIDENAV_KEY = 'pm_sidenav_expanded';

describe('AppShellComponent', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => localStorage.clear());

  describe('initial state from localStorage', () => {
    it('defaults to expanded when no value in localStorage', () => {
      const c = new AppShellComponent();
      expect(c.sidenavExpanded()).toBe(true);
    });

    it('reads collapsed (false) from localStorage before component creation', () => {
      localStorage.setItem(SIDENAV_KEY, 'false');
      const c = new AppShellComponent();
      expect(c.sidenavExpanded()).toBe(false);
    });

    it('reads expanded (true) from localStorage before component creation', () => {
      localStorage.setItem(SIDENAV_KEY, 'true');
      const c = new AppShellComponent();
      expect(c.sidenavExpanded()).toBe(true);
    });
  });

  describe('toggleSidenav()', () => {
    let component: AppShellComponent;

    beforeEach(() => {
      component = new AppShellComponent();
    });

    it('flips signal from true to false', () => {
      expect(component.sidenavExpanded()).toBe(true);
      component.toggleSidenav();
      expect(component.sidenavExpanded()).toBe(false);
    });

    it('flips signal back from false to true', () => {
      component.toggleSidenav();
      component.toggleSidenav();
      expect(component.sidenavExpanded()).toBe(true);
    });

    it('persists collapsed=false to localStorage', () => {
      component.toggleSidenav();
      expect(localStorage.getItem(SIDENAV_KEY)).toBe('false');
    });

    it('persists expanded=true to localStorage after two toggles', () => {
      component.toggleSidenav();
      component.toggleSidenav();
      expect(localStorage.getItem(SIDENAV_KEY)).toBe('true');
    });
  });
});
