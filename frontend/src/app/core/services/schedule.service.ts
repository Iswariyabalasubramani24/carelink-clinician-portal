import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ScheduleSettings } from '../models/schedule.model';
import { TransmissionScheduleEntry } from '../models/transmission-schedule.model';

@Injectable({ providedIn: 'root' })
export class ScheduleService {
  private readonly baseUrl = `${environment.apiUrl}/schedule`;

  constructor(private readonly http: HttpClient) {}

  getClinicSettings(): Observable<ScheduleSettings> {
    return this.http.get<ScheduleSettings>(`${this.baseUrl}/clinic-settings`);
  }

  updateClinicSettings(intervalDays: number): Observable<ScheduleSettings> {
    return this.http.put<ScheduleSettings>(`${this.baseUrl}/clinic-settings`, { intervalDays });
  }

  getTransmissionSchedule(): Observable<TransmissionScheduleEntry[]> {
    return this.http.get<TransmissionScheduleEntry[]>(`${this.baseUrl}/transmission-schedule`);
  }
}
