import { createFeature, createReducer, on } from '@ngrx/store';
import { DashboardFilters, DEFAULT_FILTERS, Deadline, ProjectSummary, StatCards } from '../models/dashboard.model';
import { DashboardActions } from './dashboard.actions';

export interface DashboardState {
  projects: ProjectSummary[];
  loadingProjects: boolean;
  errorProjects: string | null;
  lastUpdatedAt: number | null;
  statCards: StatCards | null;
  loadingStatCards: boolean;
  errorStatCards: string | null;
  deadlines: Deadline[];
  loadingDeadlines: boolean;
  errorDeadlines: string | null;
  filters: DashboardFilters;
}

const initialState: DashboardState = {
  projects: [],
  loadingProjects: false,
  errorProjects: null,
  lastUpdatedAt: null,
  statCards: null,
  loadingStatCards: false,
  errorStatCards: null,
  deadlines: [],
  loadingDeadlines: false,
  errorDeadlines: null,
  filters: DEFAULT_FILTERS,
};

export const dashboardFeature = createFeature({
  name: 'dashboard',
  reducer: createReducer(
    initialState,
    on(DashboardActions.startPolling, state => ({
      ...state,
      errorProjects: null,
      errorStatCards: null,
      errorDeadlines: null,
    })),
    on(DashboardActions.loadPortfolio, state => ({
      ...state,
      loadingProjects: state.projects.length === 0,
      loadingStatCards: state.statCards === null,
      loadingDeadlines: state.deadlines.length === 0,
    })),
    on(DashboardActions.loadSummarySuccess, (state, { data }) => ({
      ...state,
      loadingProjects: false,
      projects: data,
      lastUpdatedAt: Date.now(),
      errorProjects: null,
    })),
    on(DashboardActions.loadSummaryFailure, (state, { error }) => ({
      ...state,
      loadingProjects: false,
      errorProjects: error,
    })),
    on(DashboardActions.loadStatCardsSuccess, (state, { data }) => ({
      ...state,
      loadingStatCards: false,
      statCards: data,
      errorStatCards: null,
    })),
    on(DashboardActions.loadStatCardsFailure, (state, { error }) => ({
      ...state,
      loadingStatCards: false,
      errorStatCards: error,
    })),
    on(DashboardActions.loadDeadlinesSuccess, (state, { data }) => ({
      ...state,
      loadingDeadlines: false,
      deadlines: data,
      errorDeadlines: null,
    })),
    on(DashboardActions.loadDeadlinesFailure, (state, { error }) => ({
      ...state,
      loadingDeadlines: false,
      errorDeadlines: error,
    })),
    on(DashboardActions.setFilters, (state, { filters }) => ({ ...state, filters })),
    on(DashboardActions.clearFilters, state => ({ ...state, filters: DEFAULT_FILTERS })),
  ),
});
