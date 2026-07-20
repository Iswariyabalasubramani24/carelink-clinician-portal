import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Tenant } from '../models/tenant.model';

// Public endpoint - no auth token required (used pre-login on the login page).
@Injectable({ providedIn: 'root' })
export class TenantService {
  private readonly baseUrl = `${environment.apiUrl}/tenants`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Tenant[]> {
    return this.http.get<Tenant[]>(this.baseUrl);
  }
}
