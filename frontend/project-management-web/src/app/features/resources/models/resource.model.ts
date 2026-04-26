export interface Resource {
  id: string;
  code: string;
  name: string;
  email?: string;
  type: 'Inhouse' | 'Outsource';
  vendorId?: string;
  vendorName?: string;
  isActive: boolean;
  version: number;
  createdAt: string;
  createdBy: string;
  updatedAt?: string;
  updatedBy?: string;
}
