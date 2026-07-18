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
  createdAt: string;
  updatedAt?: string;
}
