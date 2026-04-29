export interface AlertDto {
  id: string;
  projectId: string | null;
  type: string;
  entityType: string | null;
  entityId: string | null;
  title: string;
  description: string | null;
  isRead: boolean;
  createdAt: string;
  readAt: string | null;
}
