export enum ReportType {
  FullReport = 'FullReport',
  SummaryReport = 'SummaryReport',
  EventReport = 'EventReport'
}

export interface Report {
  id: number;
  patientId: number;
  reportType: ReportType;
  generatedAt: string;
}

export interface ReportSettings {
  intervalDays: number;
}

export interface PatientReportSettings {
  intervalDays: number;
  isOverride: boolean;
}
