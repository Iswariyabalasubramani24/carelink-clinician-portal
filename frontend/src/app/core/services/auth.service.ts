import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, finalize, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Clinician, LoginResponse, RefreshResponse, SwitchTenantResponse } from '../models/auth.model';
import { Tenant } from '../models/tenant.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  // Access token lives in memory only - never persisted to localStorage/sessionStorage.
  // The refresh token is an httpOnly cookie the browser manages; this service never sees its value.
  private accessToken: string | null = null;

  private readonly currentClinicianSubject = new BehaviorSubject<Clinician | null>(null);
  readonly currentClinician$ = this.currentClinicianSubject.asObservable();

  private readonly isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  readonly isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.baseUrl}/login`, { email, password }, { withCredentials: true })
      .pipe(tap((response) => this.applySession(response)));
  }

  refresh(): Observable<RefreshResponse> {
    return this.http
      .post<RefreshResponse>(`${this.baseUrl}/refresh`, {}, { withCredentials: true })
      .pipe(tap((response) => this.applySession(response)));
  }

  logout(): Observable<void> {
    return this.http
      .post<void>(`${this.baseUrl}/logout`, {}, { withCredentials: true })
      .pipe(finalize(() => this.clearSession()));
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/change-password`,
      { currentPassword, newPassword },
      { withCredentials: true }
    );
  }

  getMyTenants(): Observable<Tenant[]> {
    return this.http.get<Tenant[]>(`${environment.apiUrl}/tenants/mine`);
  }

  switchTenant(tenantId: number): Observable<SwitchTenantResponse> {
    return this.http
      .post<SwitchTenantResponse>(`${this.baseUrl}/switch-tenant`, { tenantId }, { withCredentials: true })
      .pipe(tap((response) => this.applyTenantSwitch(response)));
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  isAuthenticated(): boolean {
    return !!this.accessToken;
  }

  getCurrentClinician(): Clinician | null {
    return this.currentClinicianSubject.value;
  }

  private applySession(response: LoginResponse | RefreshResponse): void {
    this.accessToken = response.accessToken;
    this.currentClinicianSubject.next({
      id: response.clinicianId,
      email: response.email,
      firstName: response.firstName,
      lastName: response.lastName,
      role: response.role,
      tenantId: response.tenantId
    });
    this.isAuthenticatedSubject.next(true);
  }

  private applyTenantSwitch(response: SwitchTenantResponse): void {
    this.accessToken = response.accessToken;
    const current = this.currentClinicianSubject.value;
    if (current) {
      this.currentClinicianSubject.next({ ...current, tenantId: response.tenantId });
    }
  }

  private clearSession(): void {
    this.accessToken = null;
    this.currentClinicianSubject.next(null);
    this.isAuthenticatedSubject.next(false);
  }
}
