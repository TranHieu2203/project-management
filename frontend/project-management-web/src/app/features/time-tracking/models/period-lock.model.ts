export interface PeriodLockDto {
  id: string;
  vendorId: string;
  year: number;
  month: number;
  lockedBy: string;
  lockedAt: string;
}

export interface PeriodReconcileDto {
  vendorId: string;
  year: number;
  month: number;
  isLocked: boolean;
  lockedAt?: string;
  estimatedHours: number;
  pmAdjustedHours: number;
  confirmedHours: number;
  confirmedCost: number;
  totalEntries: number;
}
