import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BudgetReport } from '../models/budget-report.model';

@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/reports';

  getBudgetReport(month: string, projectIds: string[]): Observable<BudgetReport> {
    let params = new HttpParams().set('month', month);
    for (const id of projectIds) params = params.append('projectIds', id);
    return this.http.get<BudgetReport>(`${this.base}/budget`, { params });
  }

  exportBudgetPdf(month: string, projectIds: string[]): Observable<Blob> {
    let params = new HttpParams().set('month', month);
    for (const id of projectIds) params = params.append('projectIds', id);
    return this.http.get(`${this.base}/budget/pdf`, { params, responseType: 'blob' });
  }

  async exportBudgetExcel(report: BudgetReport): Promise<void> {
    const { utils, writeFile } = await import('xlsx');
    const wb = utils.book_new();

    for (const section of report.projects) {
      const rows = [
        ['Vendor', 'Giờ KH', 'Giờ TT', 'Chi phí KH', 'Chi phí TT', '% XN', 'Cảnh báo'],
        ...section.vendors.map(v => [
          v.vendorName,
          v.plannedHours,
          v.actualHours,
          v.plannedCost,
          v.actualCost,
          v.confirmedPct,
          v.hasAnomaly ? '⚠' : '',
        ]),
        [],
        ['Tổng', '', '', section.totalPlannedCost, section.totalActualCost, '', ''],
      ];
      const ws = utils.aoa_to_sheet(rows);
      utils.book_append_sheet(wb, ws, section.projectName.substring(0, 31));
    }

    writeFile(wb, `budget-${report.month}.xlsx`);
  }
}
