export enum AlertType {
  IrregularHeartbeat = 'IrregularHeartbeat',
  LowBattery = 'LowBattery',
  DisconnectedMonitor = 'DisconnectedMonitor'
}

export enum AlertUrgency {
  Red = 'Red',
  Yellow = 'Yellow',
  None = 'None'
}

export interface Alert {
  id: number;
  patientId: number;
  tenantId: number;
  alertType: AlertType;
  urgency: AlertUrgency;
  triggeredAt: string;
  isAcknowledged: boolean;
  acknowledgedAt?: string;
  snoozedUntil?: string;
}

export interface PatientAlertSetting {
  alertType: AlertType;
  effectiveUrgency: AlertUrgency;
  isOverride: boolean;
}
