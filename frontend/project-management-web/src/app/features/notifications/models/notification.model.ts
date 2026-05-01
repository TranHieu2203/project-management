export interface NotificationDto {
  id: string;
  type: string;
  title: string;
  body: string;
  entityType: string | null;
  entityId: string | null;
  projectId: string | null;
  isRead: boolean;
  createdAt: string;
  readAt: string | null;
}
