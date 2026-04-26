import { ChangeDetectionStrategy, Component, inject, OnDestroy, OnInit } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { Store } from '@ngrx/store';
import { filter, Subject, switchMap, takeUntil } from 'rxjs';
import { ProjectsActions } from '../../store/projects.actions';
import {
  selectAllProjects,
  selectProjectsConflict,
  selectProjectsError,
  selectProjectsLoading,
} from '../../store/projects.selectors';
import { ProjectFormComponent, ProjectFormData } from '../project-form/project-form';
import {
  ConflictDialogComponent,
  ConflictDialogData,
} from '../../../../shared/components/conflict-dialog/conflict-dialog';
import { Project } from '../../models/project.model';

@Component({
  standalone: true,
  selector: 'app-project-list',
  imports: [
    AsyncPipe,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule,
  ],
  templateUrl: './project-list.html',
  styleUrl: './project-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectListComponent implements OnInit, OnDestroy {
  private readonly store = inject(Store);
  private readonly dialog = inject(MatDialog);
  private readonly destroy$ = new Subject<void>();

  protected readonly projects$ = this.store.select(selectAllProjects);
  protected readonly loading$ = this.store.select(selectProjectsLoading);
  protected readonly error$ = this.store.select(selectProjectsError);

  ngOnInit(): void {
    this.store.dispatch(ProjectsActions.loadProjects());
    this.watchConflict();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  retry(): void {
    this.store.dispatch(ProjectsActions.loadProjects());
  }

  openCreateDialog(): void {
    const data: ProjectFormData = { mode: 'create' };
    this.dialog.open(ProjectFormComponent, { data, width: '500px' });
  }

  openEditDialog(project: Project, event: Event): void {
    event.stopPropagation();
    const data: ProjectFormData = { mode: 'edit', project };
    this.dialog.open(ProjectFormComponent, { data, width: '500px' });
  }

  deleteProject(project: Project, event: Event): void {
    event.stopPropagation();
    this.store.dispatch(
      ProjectsActions.deleteProject({ projectId: project.id, version: project.version })
    );
  }

  private watchConflict(): void {
    this.store
      .select(selectProjectsConflict)
      .pipe(
        filter(Boolean),
        switchMap(conflict => {
          const data: ConflictDialogData = {
            serverState: conflict.serverState,
            userChanges: { name: conflict.pendingName, description: conflict.pendingDescription },
            eTag: conflict.eTag,
          };
          return this.dialog.open(ConflictDialogComponent, { data }).afterClosed();
        }),
        takeUntil(this.destroy$)
      )
      .subscribe(result => {
        if (result === 'use-server') {
          this.store.dispatch(ProjectsActions.clearConflict());
          this.store.dispatch(ProjectsActions.loadProjects());
        } else if (result === 'retry-mine') {
          this.store.dispatch(ProjectsActions.clearConflict());
        }
      });
  }
}
