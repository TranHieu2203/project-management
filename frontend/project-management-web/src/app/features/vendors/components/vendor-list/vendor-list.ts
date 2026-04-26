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
import { VendorsActions } from '../../store/vendors.actions';
import {
  selectAllVendors,
  selectVendorsLoading,
} from '../../store/vendors.selectors';
import { VendorFormComponent, VendorFormData } from '../vendor-form/vendor-form';
import { Vendor } from '../../models/vendor.model';

@Component({
  selector: 'app-vendor-list',
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
  templateUrl: './vendor-list.html',
  styleUrl: './vendor-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VendorListComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly dialog = inject(MatDialog);

  readonly vendors$ = this.store.select(selectAllVendors);
  readonly loading$ = this.store.select(selectVendorsLoading);

  readonly displayedColumns = ['code', 'name', 'description', 'status', 'actions'];

  ngOnInit(): void {
    this.store.dispatch(VendorsActions.loadVendors({}));
  }

  openCreateDialog(): void {
    const ref = this.dialog.open<VendorFormComponent, VendorFormData>(VendorFormComponent, {
      width: '480px',
      data: { vendor: null },
    });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.store.dispatch(VendorsActions.createVendor({
          code: result.code,
          name: result.name,
          description: result.description,
        }));
      }
    });
  }

  openEditDialog(vendor: Vendor): void {
    const ref = this.dialog.open<VendorFormComponent, VendorFormData>(VendorFormComponent, {
      width: '480px',
      data: { vendor },
    });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.store.dispatch(VendorsActions.updateVendor({
          vendorId: vendor.id,
          name: result.name,
          description: result.description,
          version: vendor.version,
        }));
      }
    });
  }

  inactivateVendor(vendor: Vendor): void {
    if (!confirm(`Bạn có chắc muốn inactivate vendor "${vendor.name}"?`)) return;
    this.store.dispatch(VendorsActions.inactivateVendor({
      vendorId: vendor.id,
      version: vendor.version,
    }));
  }
}
