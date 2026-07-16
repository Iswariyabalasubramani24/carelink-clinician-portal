export enum CardiacDeviceType {
  Pacemaker = 'Pacemaker',
  ImplantableCardioverterDefibrillator = 'ImplantableCardioverterDefibrillator',
  CardiacResynchronizationTherapy = 'CardiacResynchronizationTherapy',
  LoopRecorder = 'LoopRecorder'
}

export interface Patient {
  id: number;
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
