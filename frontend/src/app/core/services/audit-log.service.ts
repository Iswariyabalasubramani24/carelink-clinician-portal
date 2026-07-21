import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PagedAuditLogs } from '../models/audit-log.model';

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private readonly baseUrl = `${environment.apiUrl}/audit-logs`;

  constructor(private readonly http: HttpClient) {}

  getAll(page: number, pageSize: number, action?: string): Observable<PagedAuditLogs> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (action) {
      params = params.set('action', action);
    }

    return this.http.get<PagedAuditLogs>(this.baseUrl, { params });
  }
}
