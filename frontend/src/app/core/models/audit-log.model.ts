export interface AuditLogEntry {
  id: number;
  clinicianId: number;
  action: string;
  entityType: string;
  entityId: number;
  timestamp: string;
  details: string | null;
}

export interface PagedAuditLogs {
  items: AuditLogEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
}
