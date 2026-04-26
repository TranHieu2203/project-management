export interface ExportJobDto {
  jobId: string;
  status: 'Queued' | 'Processing' | 'Ready' | 'Failed';
  format: string;
  groupBy: string;
  fileName: string | null;
  errorMessage: string | null;
  createdAt: string;
  completedAt: string | null;
}

export interface CostBreakdownItem {
  dimensionKey: string;
  dimensionLabel: string;
  vendorId: string | null;
  vendorName: string | null;
  resourceId: string | null;
  resourceName: string | null;
  projectId: string | null;
  month: string | null;
  estimatedCost: number;
  officialCost: number;
  confirmedPct: number;
  totalHours: number;
}

export interface CostBreakdownResult {
  groupBy: string;
  totalCount: number;
  page: number;
  pageSize: number;
  items: CostBreakdownItem[];
}

export interface CostProjectBreakdown {
  projectId: string;
  estimatedCost: number;
  officialCost: number;
  vendorConfirmedCost: number;
  pmAdjustedCost: number;
  confirmedPct: number;
}

export interface CostSummaryResult {
  dateFrom: string;
  dateTo: string;
  projectCount: number;
  totalEstimatedCost: number;
  totalOfficialCost: number;
  confirmedPct: number;
  byProject: CostProjectBreakdown[];
}
