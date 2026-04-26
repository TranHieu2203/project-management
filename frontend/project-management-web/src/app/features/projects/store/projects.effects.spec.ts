import { TestBed } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Action } from '@ngrx/store';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { Project } from '../models/project.model';
import { ProjectsApiService } from '../services/projects-api.service';
import { ProjectsActions } from './projects.actions';
import { ProjectsEffects } from './projects.effects';

const mockProject: Project = {
  id: 'project-1',
  code: 'SEED-01',
  name: 'Dự Án Mẫu',
  status: 'Planning',
  visibility: 'MembersOnly',
  version: 1,
};

const mockProjects: Project[] = [mockProject];

describe('ProjectsEffects', () => {
  let actions$: Observable<Action>;
  let effects: ProjectsEffects;

  const projectsApiService = {
    getProjects: vi.fn(),
    getProjectById: vi.fn(),
    createProject: vi.fn(),
    updateProject: vi.fn(),
    deleteProject: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();

    TestBed.configureTestingModule({
      providers: [
        ProjectsEffects,
        provideMockActions(() => actions$),
        { provide: ProjectsApiService, useValue: projectsApiService },
      ],
    });

    effects = TestBed.inject(ProjectsEffects);
  });

  // ─── loadProjects$ ────────────────────────────────────────────────────────

  describe('loadProjects$', () => {
    it('dispatches loadProjectsSuccess with projects on API success', async () => {
      projectsApiService.getProjects.mockReturnValue(of(mockProjects));
      actions$ = of(ProjectsActions.loadProjects());

      const action = await firstValueFrom(effects.loadProjects$);

      expect(action).toEqual(ProjectsActions.loadProjectsSuccess({ projects: mockProjects }));
    });

    it('dispatches loadProjectsFailure with API error detail on failure', async () => {
      const apiError = { status: 500, error: { detail: 'Internal Server Error' } };
      projectsApiService.getProjects.mockReturnValue(throwError(() => apiError));
      actions$ = of(ProjectsActions.loadProjects());

      const action = await firstValueFrom(effects.loadProjects$);

      expect(action).toEqual(
        ProjectsActions.loadProjectsFailure({ error: 'Internal Server Error' })
      );
    });

    it('dispatches loadProjectsFailure with API error title when no detail', async () => {
      const apiError = { status: 503, error: { title: 'Service Unavailable' } };
      projectsApiService.getProjects.mockReturnValue(throwError(() => apiError));
      actions$ = of(ProjectsActions.loadProjects());

      const action = await firstValueFrom(effects.loadProjects$);

      expect(action).toEqual(
        ProjectsActions.loadProjectsFailure({ error: 'Service Unavailable' })
      );
    });

    it('dispatches loadProjectsFailure with fallback message on network error', async () => {
      projectsApiService.getProjects.mockReturnValue(throwError(() => ({ status: 0, error: null })));
      actions$ = of(ProjectsActions.loadProjects());

      const action = await firstValueFrom(effects.loadProjects$);

      expect(action).toEqual(
        ProjectsActions.loadProjectsFailure({
          error: 'Không thể tải danh sách dự án. Vui lòng thử lại.',
        })
      );
    });
  });

  // ─── createProject$ ───────────────────────────────────────────────────────

  describe('createProject$', () => {
    it('dispatches createProjectSuccess on API success', async () => {
      projectsApiService.createProject.mockReturnValue(of(mockProject));
      actions$ = of(ProjectsActions.createProject({ code: 'TEST-01', name: 'Test' }));

      const action = await firstValueFrom(effects.createProject$);

      expect(action).toEqual(ProjectsActions.createProjectSuccess({ project: mockProject }));
      expect(projectsApiService.createProject).toHaveBeenCalledWith('TEST-01', 'Test', undefined);
    });

    it('dispatches createProjectFailure on API failure', async () => {
      const apiError = { status: 409, error: { detail: 'Code đã tồn tại' } };
      projectsApiService.createProject.mockReturnValue(throwError(() => apiError));
      actions$ = of(ProjectsActions.createProject({ code: 'DUP-01', name: 'Duplicate' }));

      const action = await firstValueFrom(effects.createProject$);

      expect(action).toEqual(
        ProjectsActions.createProjectFailure({ error: 'Code đã tồn tại' })
      );
    });

    it('dispatches createProjectSuccess with description', async () => {
      const projectWithDesc: Project = { ...mockProject, description: 'A desc' };
      projectsApiService.createProject.mockReturnValue(of(projectWithDesc));
      actions$ = of(
        ProjectsActions.createProject({ code: 'TST-01', name: 'Test', description: 'A desc' })
      );

      const action = await firstValueFrom(effects.createProject$);

      expect(action).toEqual(ProjectsActions.createProjectSuccess({ project: projectWithDesc }));
      expect(projectsApiService.createProject).toHaveBeenCalledWith('TST-01', 'Test', 'A desc');
    });
  });

  // ─── updateProject$ ───────────────────────────────────────────────────────

  describe('updateProject$', () => {
    it('dispatches updateProjectSuccess on API success', async () => {
      const updated: Project = { ...mockProject, name: 'Updated', version: 2 };
      projectsApiService.updateProject.mockReturnValue(of(updated));
      actions$ = of(
        ProjectsActions.updateProject({ projectId: 'project-1', name: 'Updated', version: 1 })
      );

      const action = await firstValueFrom(effects.updateProject$);

      expect(action).toEqual(ProjectsActions.updateProjectSuccess({ project: updated }));
    });

    it('dispatches updateProjectConflict on 409 — current/eTag at root level', async () => {
      const serverState: Project = { ...mockProject, name: 'Server State', version: 2 };
      const conflictError = {
        status: 409,
        error: {
          current: serverState,   // root level, không phải extensions.current
          eTag: '"2"',            // root level
          detail: 'Conflict',
        },
      };
      projectsApiService.updateProject.mockReturnValue(throwError(() => conflictError));
      actions$ = of(
        ProjectsActions.updateProject({ projectId: 'project-1', name: 'My Change', version: 1 })
      );

      const action = await firstValueFrom(effects.updateProject$);

      expect(action).toEqual(
        ProjectsActions.updateProjectConflict({
          serverState,
          eTag: '"2"',
          pendingName: 'My Change',
          pendingDescription: undefined,
        })
      );
    });

    it('dispatches updateProjectFailure on other errors', async () => {
      const apiError = { status: 404, error: { detail: 'Not found' } };
      projectsApiService.updateProject.mockReturnValue(throwError(() => apiError));
      actions$ = of(
        ProjectsActions.updateProject({ projectId: 'project-1', name: 'X', version: 1 })
      );

      const action = await firstValueFrom(effects.updateProject$);

      expect(action).toEqual(
        ProjectsActions.updateProjectFailure({ error: 'Not found' })
      );
    });
  });

  // ─── deleteProject$ ───────────────────────────────────────────────────────

  describe('deleteProject$', () => {
    it('dispatches deleteProjectSuccess on API success', async () => {
      projectsApiService.deleteProject.mockReturnValue(of(void 0));
      actions$ = of(ProjectsActions.deleteProject({ projectId: 'project-1', version: 1 }));

      const action = await firstValueFrom(effects.deleteProject$);

      expect(action).toEqual(ProjectsActions.deleteProjectSuccess({ projectId: 'project-1' }));
    });

    it('dispatches deleteProjectFailure on API error', async () => {
      const apiError = { status: 409, error: { detail: 'Conflict' } };
      projectsApiService.deleteProject.mockReturnValue(throwError(() => apiError));
      actions$ = of(ProjectsActions.deleteProject({ projectId: 'project-1', version: 1 }));

      const action = await firstValueFrom(effects.deleteProject$);

      expect(action).toEqual(
        ProjectsActions.deleteProjectFailure({ error: 'Conflict' })
      );
    });
  });
});
