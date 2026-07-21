export interface Hospital {
  id: number;
  name: string;
  region: string;
  languageCode: string;
  isActive: boolean;
  clinicianCount: number;
}

export interface ProvisionHospitalResult {
  hospital: Hospital;
  adminEmail: string;
  temporaryPassword: string;
}
