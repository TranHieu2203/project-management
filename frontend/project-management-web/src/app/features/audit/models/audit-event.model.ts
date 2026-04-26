export interface AuditEvent {
  id: string;
  entityType: string;
  entityId: string;
  action: string;
  actor: string;
  summary: string;
  createdAt: string;
}
