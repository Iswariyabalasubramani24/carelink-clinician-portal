import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Alert, PatientAlertSetting } from '../models/alert.model';
import { Patient, PatientSearchFilters } from '../models/patient.model';
import { PatientNote } from '../models/patient-note.model';
import { PatientReportSettings, Report, ReportType } from '../models/report.model';
import { PatientScheduleSettings } from '../models/schedule.model';
import { TransmissionHistoryPoint } from '../models/transmission-history.model';

export interface UpdatePatientAlertSettingsPayload {
  useOverride: boolean;
  overrides: { alertType: string; urgency: string }[];
}

export interface UpdatePatientReportSettingsPayload {
  useOverride: boolean;
  intervalDays: number;
}

export interface UpdatePatientScheduleSettingsPayload {
  useOverride: boolean;
  intervalDays: number;
}

@Injectable({ providedIn: 'root' })
export class PatientService {
  private readonly baseUrl = `${environment.apiUrl}/patients`;

  constructor(private readonly http: HttpClient) {}

  // tenantId is derived server-side from the JWT access token - no longer sent by the client.
  getAll(filters?: PatientSearchFilters): Observable<Patient[]> {
    let params = new HttpParams();
    if (filters?.deviceType) {
      params = params.set('deviceType', filters.deviceType);
    }
    if (filters?.implantDateFrom) {
      params = params.set('implantDateFrom', filters.implantDateFrom);
    }
    if (filters?.implantDateTo) {
      params = params.set('implantDateTo', filters.implantDateTo);
    }
    if (filters?.isActive !== undefined) {
      params = params.set('isActive', String(filters.isActive));
    }
    if (filters?.keyword) {
      params = params.set('keyword', filters.keyword);
    }

    return this.http.get<Patient[]>(this.baseUrl, { params });
  }

  getById(id: number): Observable<Patient> {
    return this.http.get<Patient>(`${this.baseUrl}/${id}`);
  }

  create(patient: Partial<Patient>): Observable<Patient> {
    return this.http.post<Patient>(this.baseUrl, patient);
  }

  update(id: number, patient: Partial<Patient>): Observable<Patient> {
    return this.http.put<Patient>(`${this.baseUrl}/${id}`, patient);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  getTransmissionHistory(id: number): Observable<TransmissionHistoryPoint[]> {
    return this.http.get<TransmissionHistoryPoint[]>(`${this.baseUrl}/${id}/transmissions`);
  }

  getAlerts(id: number): Observable<Alert[]> {
    return this.http.get<Alert[]>(`${this.baseUrl}/${id}/alerts`);
  }

  getAlertSettings(id: number): Observable<PatientAlertSetting[]> {
    return this.http.get<PatientAlertSetting[]>(`${this.baseUrl}/${id}/alert-settings`);
  }

  updateAlertSettings(id: number, payload: UpdatePatientAlertSettingsPayload): Observable<PatientAlertSetting[]> {
    return this.http.put<PatientAlertSetting[]>(`${this.baseUrl}/${id}/alert-settings`, payload);
  }

  getReports(id: number): Observable<Report[]> {
    return this.http.get<Report[]>(`${this.baseUrl}/${id}/reports`);
  }

  generateReport(id: number, reportType: ReportType): Observable<Report> {
    return this.http.post<Report>(`${this.baseUrl}/${id}/reports`, { reportType });
  }

  getReportSettings(id: number): Observable<PatientReportSettings> {
    return this.http.get<PatientReportSettings>(`${this.baseUrl}/${id}/report-settings`);
  }

  updateReportSettings(id: number, payload: UpdatePatientReportSettingsPayload): Observable<PatientReportSettings> {
    return this.http.put<PatientReportSettings>(`${this.baseUrl}/${id}/report-settings`, payload);
  }

  getScheduleSettings(id: number): Observable<PatientScheduleSettings> {
    return this.http.get<PatientScheduleSettings>(`${this.baseUrl}/${id}/schedule-settings`);
  }

  updateScheduleSettings(id: number, payload: UpdatePatientScheduleSettingsPayload): Observable<PatientScheduleSettings> {
    return this.http.put<PatientScheduleSettings>(`${this.baseUrl}/${id}/schedule-settings`, payload);
  }

  getNotes(id: number): Observable<PatientNote[]> {
    return this.http.get<PatientNote[]>(`${this.baseUrl}/${id}/notes`);
  }

  createNote(id: number, content: string): Observable<PatientNote> {
    return this.http.post<PatientNote>(`${this.baseUrl}/${id}/notes`, { content });
  }
}
