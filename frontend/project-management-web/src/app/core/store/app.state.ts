import { ActionReducerMap } from '@ngrx/store';
import { routerReducer, RouterReducerState } from '@ngrx/router-store';
import { authReducer, AuthState } from '../../features/auth/store/auth.reducer';
import { projectsReducer, ProjectsState } from '../../features/projects/store/projects.reducer';
import { tasksReducer, TasksState } from '../../features/projects/store/tasks.reducer';
import { ganttReducer } from '../../features/gantt/store/gantt.reducer';
import { GanttState } from '../../features/gantt/models/gantt.model';
import { vendorsReducer, VendorsState } from '../../features/vendors/store/vendors.reducer';
import { resourcesReducer, ResourcesState } from '../../features/resources/store/resources.reducer';
import { lookupsReducer, LookupsState } from '../../features/lookups/store/lookups.reducer';
import { ratesReducer, RatesState } from '../../features/rates/store/rates.reducer';
import { timeTrackingReducer, TimeTrackingState } from '../../features/time-tracking/store/time-tracking.reducer';
import { capacityFeature, CapacityState } from '../../features/capacity/store/capacity.reducer';
import { reportingFeature, ReportingState } from '../../features/reporting/store/reporting.reducer';
import { dashboardFeature, DashboardState } from '../../features/dashboard/store/dashboard.reducer';
import { alertsFeature, AlertsState } from '../../features/alerts/store/alert.reducer';
import { notificationsFeature, NotificationsState } from '../../features/notifications/store/notification.reducer';

export interface AppState {
  router: RouterReducerState;
  auth: AuthState;
  projects: ProjectsState;
  tasks: TasksState;
  gantt: GanttState;
  vendors: VendorsState;
  resources: ResourcesState;
  lookups: LookupsState;
  rates: RatesState;
  timeTracking: TimeTrackingState;
  capacity: CapacityState;
  reporting: ReportingState;
  dashboard: DashboardState;
  alerts: AlertsState;
  notifications: NotificationsState;
}

export const reducers: ActionReducerMap<AppState> = {
  router: routerReducer,
  auth: authReducer,
  projects: projectsReducer,
  tasks: tasksReducer,
  gantt: ganttReducer,
  vendors: vendorsReducer,
  resources: resourcesReducer,
  lookups: lookupsReducer,
  rates: ratesReducer,
  timeTracking: timeTrackingReducer,
  capacity: capacityFeature.reducer,
  reporting: reportingFeature.reducer,
  dashboard: dashboardFeature.reducer,
  alerts: alertsFeature.reducer,
  notifications: notificationsFeature.reducer,
};
