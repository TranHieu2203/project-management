export interface ImportJobDto {
  id: string;
  vendorId: string;
  fileName: string;
  fileHash: string;
  status: string;
  totalRows: number;
  errorCount: number;
  enteredBy: string;
  createdAt: string;
  completedAt?: string;
}

export interface ImportJobErrorDto {
  id: string;
  importJobId: string;
  rowIndex: number;
  columnName?: string;
  errorType: 'blocking' | 'warning';
  message: string;
}

export interface ColumnMapping {
  resourceIdColumn: string;
  projectIdColumn: string;
  dateColumn: string;
  hoursColumn: string;
  roleColumn: string;
  levelColumn: string;
  noteColumn?: string;
  taskIdColumn?: string;
}
