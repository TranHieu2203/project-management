import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { createReducer, on } from '@ngrx/store';
import { Project } from '../models/project.model';
import { ProjectsActions } from './projects.actions';

export interface ConflictState {
  serverState: Project;
  eTag: string;
  pendingName: string;
  pendingDescription?: string;
}

export interface ProjectsState extends EntityState<Project> {
  selectedId: string | null;
  loading: boolean;
  error: string | null;
  creating: boolean;
  updating: boolean;
  deleting: boolean;
  conflict: ConflictState | null;
}

export const projectsAdapter = createEntityAdapter<Project>();

const initialState: ProjectsState = projectsAdapter.getInitialState({
  selectedId: null,
  loading: false,
  error: null,
  creating: false,
  updating: false,
  deleting: false,
  conflict: null,
});

export const projectsReducer = createReducer(
  initialState,

  // Load
  on(ProjectsActions.loadProjects, state => ({
    ...state,
    loading: true,
    error: null,
  })),
  on(ProjectsActions.loadProjectsSuccess, (state, { projects }) =>
    projectsAdapter.setAll(projects, { ...state, loading: false, error: null })
  ),
  on(ProjectsActions.loadProjectsFailure, (state, { error }) => ({
    ...state,
    loading: false,
    error,
  })),
  on(ProjectsActions.selectProject, (state, { projectId }) => ({
    ...state,
    selectedId: projectId,
  })),

  // Create
  on(ProjectsActions.createProject, state => ({
    ...state,
    creating: true,
    error: null,
  })),
  on(ProjectsActions.createProjectSuccess, (state, { project }) =>
    projectsAdapter.addOne(project, { ...state, creating: false })
  ),
  on(ProjectsActions.createProjectFailure, (state, { error }) => ({
    ...state,
    creating: false,
    error,
  })),

  // Update
  on(ProjectsActions.updateProject, state => ({
    ...state,
    updating: true,
    error: null,
  })),
  on(ProjectsActions.updateProjectSuccess, (state, { project }) =>
    projectsAdapter.updateOne(
      { id: project.id, changes: project },
      { ...state, updating: false }
    )
  ),
  on(ProjectsActions.updateProjectFailure, (state, { error }) => ({
    ...state,
    updating: false,
    error,
  })),
  on(ProjectsActions.updateProjectConflict, (state, { serverState, eTag, pendingName, pendingDescription }) => ({
    ...state,
    updating: false,
    conflict: { serverState, eTag, pendingName, pendingDescription },
  })),

  // Delete
  on(ProjectsActions.deleteProject, state => ({
    ...state,
    deleting: true,
    error: null,
  })),
  on(ProjectsActions.deleteProjectSuccess, (state, { projectId }) =>
    projectsAdapter.removeOne(projectId, { ...state, deleting: false })
  ),
  on(ProjectsActions.deleteProjectFailure, (state, { error }) => ({
    ...state,
    deleting: false,
    error,
  })),

  // Clear conflict
  on(ProjectsActions.clearConflict, state => ({
    ...state,
    conflict: null,
  }))
);
