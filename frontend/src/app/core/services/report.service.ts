import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ReportSettings } from '../models/report.model';

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly baseUrl = `${environment.apiUrl}/reports`;

  constructor(private readonly http: HttpClient) {}

  downloadUrl(reportId: number): string {
    return `${this.baseUrl}/${reportId}/download`;
  }

  download(reportId: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${reportId}/download`, { responseType: 'blob' });
  }

  getClinicSettings(): Observable<ReportSettings> {
    return this.http.get<ReportSettings>(`${this.baseUrl}/clinic-settings`);
  }

  updateClinicSettings(intervalDays: number): Observable<ReportSettings> {
    return this.http.put<ReportSettings>(`${this.baseUrl}/clinic-settings`, { intervalDays });
  }
}
