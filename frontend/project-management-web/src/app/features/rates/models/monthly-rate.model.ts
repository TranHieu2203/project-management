export interface MonthlyRate {
  id: string;
  vendorId: string;
  vendorName?: string;
  role: string;
  level: string;
  year: number;
  month: number;
  monthlyAmount: number;
  hourlyRate: number;
  createdAt: string;
  createdBy: string;
}
