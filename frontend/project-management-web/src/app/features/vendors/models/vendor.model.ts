export interface Vendor {
  id: string;
  code: string;
  name: string;
  description?: string;
  isActive: boolean;
  version: number;
  createdAt: string;
  createdBy: string;
  updatedAt?: string;
  updatedBy?: string;
}
