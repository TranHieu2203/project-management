export interface BudgetVendorRow {
  vendorId: string | null;
  vendorName: string;
  plannedHours: number;
  actualHours: number;
  plannedCost: number;
  actualCost: number;
  confirmedPct: number;
  hasAnomaly: boolean;
}

export interface BudgetProjectSection {
  projectId: string;
  projectName: string;
  totalPlannedCost: number;
  totalActualCost: number;
  vendors: BudgetVendorRow[];
}

export interface BudgetReport {
  month: string;
  workingDaysInMonth: number;
  grandTotalPlanned: number;
  grandTotalActual: number;
  projects: BudgetProjectSection[];
}

export interface ReportsFilters {
  month: string;
  projectIds: string[];
}

export interface ReportsState {
  filters: ReportsFilters;
  report: BudgetReport | null;
  loading: boolean;
  error: string | null;
}
