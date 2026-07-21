import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Alert, PatientAlertSetting } from '../models/alert.model';
import { Patient } from '../models/patient.model';
import { TransmissionHistoryPoint } from '../models/transmission-history.model';

export interface UpdatePatientAlertSettingsPayload {
  useOverride: boolean;
  overrides: { alertType: string; urgency: string }[];
}

@Injectable({ providedIn: 'root' })
export class PatientService {
  private readonly baseUrl = `${environment.apiUrl}/patients`;

  constructor(private readonly http: HttpClient) {}

  // tenantId is derived server-side from the JWT access token - no longer sent by the client.
  getAll(): Observable<Patient[]> {
    return this.http.get<Patient[]>(this.baseUrl);
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
}
