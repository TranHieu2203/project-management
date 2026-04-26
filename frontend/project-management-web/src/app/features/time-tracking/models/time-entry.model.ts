export interface TimeEntry {
  id: string;
  resourceId: string;
  projectId: string;
  taskId?: string;
  date: string;
  hours: number;
  entryType: string;
  note?: string;
  rateAtTime: number;
  costAtTime: number;
  enteredBy: string;
  createdAt: string;
  isVoided: boolean;
  voidReason?: string;
  voidedBy?: string;
  voidedAt?: string;
  supersedesId?: string;
}
