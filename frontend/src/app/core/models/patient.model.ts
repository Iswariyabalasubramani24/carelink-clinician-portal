export enum CardiacDeviceType {
  ICD = 'ICD',
  Pacemaker = 'Pacemaker',
  CRT_P = 'CRT_P',
  CRT_D = 'CRT_D',
  ICM = 'ICM'
}

export interface Patient {
  id: number;
  tenantId: number;
  medicalRecordNumber: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  phoneNumber?: string;
  email?: string;
  deviceType: CardiacDeviceType;
  deviceManufacturer?: string;
  deviceModel?: string;
  deviceSerialNumber: string;
  implantDate: string;
  batteryLevel?: number;
  lastHeartRate?: number;
  lastSyncedAt?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

// All fields optional and combinable (AND logic) - mirrors the backend's
// PatientSearchFilters. Date fields are yyyy-MM-dd strings from <input type="date">.
export interface PatientSearchFilters {
  deviceType?: CardiacDeviceType;
  implantDateFrom?: string;
  implantDateTo?: string;
  isActive?: boolean;
  keyword?: string;
}
