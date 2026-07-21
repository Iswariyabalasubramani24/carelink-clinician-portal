export interface TransmissionScheduleEntry {
  patientId: number;
  patientName: string;
  lastSyncedAt: string | null;
  intervalDays: number;
  nextScheduledDate: string | null;
}
