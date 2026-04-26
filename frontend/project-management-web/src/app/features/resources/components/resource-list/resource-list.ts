import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ResourcesActions } from '../../store/resources.actions';
import { selectAllResources, selectResourcesLoading } from '../../store/resources.selectors';
import { ResourceFormComponent, ResourceFormData } from '../resource-form/resource-form';
import { Resource } from '../../models/resource.model';

@Component({
  selector: 'app-resource-list',
  standalone: true,
  imports: [
    AsyncPipe,
    MatButtonModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatTooltipModule,
  ],
  templateUrl: './resource-list.html',
  styleUrl: './resource-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourceListComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly dialog = inject(MatDialog);

  readonly resources$ = this.store.select(selectAllResources);
  readonly loading$ = this.store.select(selectResourcesLoading);

  readonly displayedColumns = ['code', 'name', 'email', 'type', 'vendor', 'status', 'actions'];

  ngOnInit(): void {
    this.store.dispatch(ResourcesActions.loadResources({}));
  }

  openCreateDialog(): void {
    const ref = this.dialog.open<ResourceFormComponent, ResourceFormData>(ResourceFormComponent, {
      width: '520px',
      data: { resource: null },
    });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.store.dispatch(ResourcesActions.createResource({
          code: result.code,
          name: result.name,
          email: result.email,
          resourceType: result.type,
          vendorId: result.vendorId,
        }));
      }
    });
  }

  openEditDialog(resource: Resource): void {
    const ref = this.dialog.open<ResourceFormComponent, ResourceFormData>(ResourceFormComponent, {
      width: '520px',
      data: { resource },
    });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.store.dispatch(ResourcesActions.updateResource({
          resourceId: resource.id,
          name: result.name,
          email: result.email,
          version: resource.version,
        }));
      }
    });
  }

  inactivateResource(resource: Resource): void {
    if (!confirm(`Bạn có chắc muốn inactivate resource "${resource.name}"?`)) return;
    this.store.dispatch(ResourcesActions.inactivateResource({
      resourceId: resource.id,
      version: resource.version,
    }));
  }
}
