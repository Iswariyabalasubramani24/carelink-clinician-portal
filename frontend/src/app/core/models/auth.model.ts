export interface Clinician {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  tenantId: number;
}

export interface LoginResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  clinicianId: number;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  tenantId: number;
}

export interface RefreshResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  clinicianId: number;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  tenantId: number;
}
