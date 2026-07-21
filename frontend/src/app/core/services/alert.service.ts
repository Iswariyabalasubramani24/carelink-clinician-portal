import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Alert } from '../models/alert.model';

@Injectable({ providedIn: 'root' })
export class AlertService {
  private readonly baseUrl = `${environment.apiUrl}/alerts`;

  constructor(private readonly http: HttpClient) {}

  getActive(): Observable<Alert[]> {
    return this.http.get<Alert[]>(this.baseUrl);
  }

  acknowledge(alertId: number): Observable<Alert> {
    return this.http.post<Alert>(`${this.baseUrl}/${alertId}/acknowledge`, {});
  }

  snooze(alertId: number): Observable<Alert> {
    return this.http.post<Alert>(`${this.baseUrl}/${alertId}/snooze`, {});
  }
}
