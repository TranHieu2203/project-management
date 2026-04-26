import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AsyncPipe, DatePipe } from '@angular/common';
import { Observable } from 'rxjs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { AuditApiService } from '../../services/audit-api.service';
import { AuditEvent } from '../../models/audit-event.model';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [AsyncPipe, DatePipe, MatProgressSpinnerModule, MatTableModule],
  templateUrl: './audit-log.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuditLogComponent implements OnInit {
  private readonly auditApi = inject(AuditApiService);

  auditEvents$!: Observable<AuditEvent[]>;

  readonly displayedColumns = ['entityType', 'entityId', 'action', 'actor', 'summary', 'createdAt'];

  ngOnInit(): void {
    this.auditEvents$ = this.auditApi.getAuditEvents();
  }
}
