import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Hospital, ProvisionHospitalResult } from '../models/hospital.model';

export interface ProvisionHospitalPayload {
  name: string;
  region: string;
  languageCode: string;
  adminFirstName: string;
  adminLastName: string;
  adminEmail: string;
}

// SuperAdmin-only platform operations; the backend rejects any other role.
@Injectable({ providedIn: 'root' })
export class HospitalService {
  private readonly baseUrl = `${environment.apiUrl}/hospitals`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Hospital[]> {
    return this.http.get<Hospital[]>(this.baseUrl);
  }

  provision(payload: ProvisionHospitalPayload): Observable<ProvisionHospitalResult> {
    return this.http.post<ProvisionHospitalResult>(this.baseUrl, payload);
  }
}
