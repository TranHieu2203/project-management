import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { createReducer, on } from '@ngrx/store';
import { ProjectTask } from '../models/task.model';
import { FilterCriteria } from '../models/filter.model';
import { removeCriterion } from '../models/filter.utils';
import { TasksActions } from './tasks.actions';

export interface TaskConflictState {
  serverState: ProjectTask;
  eTag: string;
}

export interface TasksState extends EntityState<ProjectTask> {
  currentProjectId: string | null;
  selectedTaskId: string | null;
  loading: boolean;
  creating: boolean;
  updating: boolean;
  deleting: boolean;
  error: string | null;
  conflict: TaskConflictState | null;
  /** Filter criteria hiện tại — source of truth, serialize thành URL params */
  activeFilter: FilterCriteria;
}

export const tasksAdapter = createEntityAdapter<ProjectTask>();

export const initialState: TasksState = tasksAdapter.getInitialState({
  currentProjectId: null,
  selectedTaskId: null,
  loading: false,
  creating: false,
  updating: false,
  deleting: false,
  error: null,
  conflict: null,
  activeFilter: {},
});

export const tasksReducer = createReducer(
    initialState,

    // Load
    on(TasksActions.loadTasks, (s, { projectId }) =>
      ({ ...s, loading: true, error: null, currentProjectId: projectId })),
    on(TasksActions.loadTasksSuccess, (s, { tasks }) =>
      tasksAdapter.setAll(tasks, { ...s, loading: false })),
    on(TasksActions.loadTasksFailure, (s, { error }) =>
      ({ ...s, loading: false, error })),

    // Create
    on(TasksActions.createTask, s => ({ ...s, creating: true, error: null })),
    on(TasksActions.createTaskSuccess, (s, { task }) =>
      tasksAdapter.addOne(task, { ...s, creating: false })),
    on(TasksActions.createTaskFailure, (s, { error }) =>
      ({ ...s, creating: false, error })),

    // Update
    on(TasksActions.updateTask, s => ({ ...s, updating: true, error: null })),
    on(TasksActions.updateTaskSuccess, (s, { task }) =>
      tasksAdapter.upsertOne(task, { ...s, updating: false, conflict: null })),
    on(TasksActions.updateTaskFailure, (s, { error }) =>
      ({ ...s, updating: false, error })),
    on(TasksActions.updateTaskConflict, (s, { serverState, eTag }) =>
      ({ ...s, updating: false, conflict: { serverState, eTag } })),

    // Delete
    on(TasksActions.deleteTask, s => ({ ...s, deleting: true, error: null })),
    on(TasksActions.deleteTaskSuccess, (s, { taskId }) =>
      tasksAdapter.removeOne(taskId, { ...s, deleting: false })),
    on(TasksActions.deleteTaskFailure, (s, { error }) =>
      ({ ...s, deleting: false, error })),

    // Filter
    on(TasksActions.setFilter, (s, { criteria }) =>
      ({ ...s, activeFilter: criteria })),
    on(TasksActions.clearFilter, s =>
      ({ ...s, activeFilter: {} })),
    on(TasksActions.clearOneCriterion, (s, { key }) =>
      ({ ...s, activeFilter: removeCriterion(s.activeFilter, key) })),

    // Misc
    on(TasksActions.clearTaskConflict, s => ({ ...s, conflict: null })),
    on(TasksActions.selectTask, (s, { taskId }) => ({ ...s, selectedTaskId: taskId })),
    on(TasksActions.clearTasks, () => tasksAdapter.removeAll({ ...initialState })),
);
