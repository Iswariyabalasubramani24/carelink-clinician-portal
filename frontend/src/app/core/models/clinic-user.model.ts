export enum ClinicianRole {
  Clinician = 'Clinician',
  Admin = 'Admin'
}

export interface ClinicUser {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  role: ClinicianRole;
  languageCode: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateClinicUserResult {
  user: ClinicUser;
  temporaryPassword: string;
}

export interface ResetClinicianPasswordResult {
  user: ClinicUser;
  temporaryPassword: string;
}
