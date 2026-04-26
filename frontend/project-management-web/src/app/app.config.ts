import { ApplicationConfig, isDevMode, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideStore } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { provideStoreDevtools } from '@ngrx/store-devtools';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { retryInterceptor } from './core/interceptors/retry.interceptor';
import { reducers } from './core/store/app.state';
import { AuthEffects } from './features/auth/store/auth.effects';
import { ProjectsEffects } from './features/projects/store/projects.effects';
import { TasksEffects } from './features/projects/store/tasks.effects';
import { GanttEffects } from './features/gantt/store/gantt.effects';
import { VendorsEffects } from './features/vendors/store/vendors.effects';
import { ResourcesEffects } from './features/resources/store/resources.effects';
import { LookupsEffects } from './features/lookups/store/lookups.effects';
import { RatesEffects } from './features/rates/store/rates.effects';
import { TimeTrackingEffects } from './features/time-tracking/store/time-tracking.effects';
import { CapacityEffects } from './features/capacity/store/capacity.effects';
import { ReportingEffects } from './features/reporting/store/reporting.effects';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([authInterceptor, errorInterceptor, retryInterceptor])
    ),
    provideAnimationsAsync(),
    provideStore(reducers),
    provideEffects([AuthEffects, ProjectsEffects, TasksEffects, GanttEffects, VendorsEffects, ResourcesEffects, LookupsEffects, RatesEffects, TimeTrackingEffects, CapacityEffects, ReportingEffects]),
    provideStoreDevtools({ maxAge: 25, logOnly: !isDevMode() }),
  ],
};
