import { createFeature, createReducer, on } from '@ngrx/store';
import { AlertDto } from '../models/alert.model';
import { AlertActions } from './alert.actions';

export interface AlertsState {
  alerts: AlertDto[];
  loading: boolean;
  panelOpen: boolean;
  unreadCount: number;
}

const initialState: AlertsState = {
  alerts: [],
  loading: false,
  panelOpen: false,
  unreadCount: 0,
};

export const alertsFeature = createFeature({
  name: 'alerts',
  reducer: createReducer(
    initialState,
    on(AlertActions.loadAlerts, state => ({ ...state, loading: true })),
    on(AlertActions.loadAlertsSuccess, (state, { alerts }) => ({
      ...state,
      loading: false,
      alerts,
      unreadCount: alerts.filter(a => !a.isRead).length,
    })),
    on(AlertActions.loadAlertsFailure, state => ({ ...state, loading: false })),
    on(AlertActions.markAlertReadSuccess, (state, { id }) => {
      const alerts = state.alerts.map(a =>
        a.id === id ? { ...a, isRead: true, readAt: new Date().toISOString() } : a
      );
      return { ...state, alerts, unreadCount: alerts.filter(a => !a.isRead).length };
    }),
    on(AlertActions.togglePanel, state => ({ ...state, panelOpen: !state.panelOpen })),
    on(AlertActions.closePanel, state => ({ ...state, panelOpen: false })),
  ),
});

export const {
  selectAlerts,
  selectLoading,
  selectPanelOpen,
  selectUnreadCount,
} = alertsFeature;
