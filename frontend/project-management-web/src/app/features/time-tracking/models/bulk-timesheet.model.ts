export interface BulkTimesheetRow {
  resourceId: string;
  projectId: string;
  taskId?: string;
  date: string;
  hours: number;
  entryType: string;
  role: string;
  level: string;
  note?: string;
}

export interface BulkValidationError {
  rowIndex: number;
  errorType: 'hard' | 'warning';
  message: string;
}
