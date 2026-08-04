import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ClinicianRole,
  ClinicUser,
  CreateClinicUserResult,
  ResetClinicianPasswordResult
} from '../models/clinic-user.model';

export interface CreateClinicUserPayload {
  firstName: string;
  lastName: string;
  email: string;
  languageCode: string;
  role: ClinicianRole;
}

@Injectable({ providedIn: 'root' })
export class ClinicUserService {
  private readonly baseUrl = `${environment.apiUrl}/clinic-users`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<ClinicUser[]> {
    return this.http.get<ClinicUser[]>(this.baseUrl);
  }

  create(payload: CreateClinicUserPayload): Observable<CreateClinicUserResult> {
    return this.http.post<CreateClinicUserResult>(this.baseUrl, payload);
  }

  suspend(id: number): Observable<ClinicUser> {
    return this.http.put<ClinicUser>(`${this.baseUrl}/${id}/suspend`, {});
  }

  activate(id: number): Observable<ClinicUser> {
    return this.http.put<ClinicUser>(`${this.baseUrl}/${id}/activate`, {});
  }

  resetPassword(id: number): Observable<ResetClinicianPasswordResult> {
    return this.http.put<ResetClinicianPasswordResult>(`${this.baseUrl}/${id}/reset-password`, {});
  }
}
