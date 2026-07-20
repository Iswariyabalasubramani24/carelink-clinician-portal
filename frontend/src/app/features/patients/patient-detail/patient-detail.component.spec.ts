import 'jest-canvas-mock';

// jsdom has no ResizeObserver, but Chart.js's `responsive: true` option needs
// one to attach its resize listener - stub it so chart construction doesn't throw.
class ResizeObserverMock {
  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
}
(globalThis as unknown as { ResizeObserver: unknown }).ResizeObserver = ResizeObserverMock;

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { of } from 'rxjs';

import { CardiacDeviceType, Patient } from '../../../core/models/patient.model';
import { TransmissionHistoryPoint } from '../../../core/models/transmission-history.model';
import { PatientService } from '../../../core/services/patient.service';
import { useEnglishTestTranslations } from '../../../core/testing/translate-testing';
import { selectAllPatients, selectPatientsLoading } from '../../../store/patients/patients.selectors';
import { PatientDetailComponent } from './patient-detail.component';

describe('PatientDetailComponent', () => {
  let fixture: ComponentFixture<PatientDetailComponent>;
  let store: MockStore;
  let patientServiceMock: { getTransmissionHistory: jest.Mock };

  const mockPatient: Patient = {
    id: 1,
    tenantId: 1,
    medicalRecordNumber: 'APL-1001',
    firstName: 'Rajesh',
    lastName: 'Kumar',
    dateOfBirth: '1965-04-12',
    deviceType: CardiacDeviceType.ICD,
    deviceManufacturer: 'Medtronic',
    deviceModel: 'Evera XT',
    deviceSerialNumber: 'MDT-ICD-0001',
    implantDate: '2022-03-15',
    batteryLevel: 87.5,
    lastHeartRate: 72,
    isActive: true,
    createdAt: '2026-07-17T20:34:52Z'
  };

  const mockHistory: TransmissionHistoryPoint[] = [
    { date: '2026-04-22', heartRate: 70, batteryLevel: 92 },
    { date: '2026-07-20', heartRate: 72, batteryLevel: 87.5 }
  ];

  async function setup(patientId = '1'): Promise<void> {
    patientServiceMock = { getTransmissionHistory: jest.fn().mockReturnValue(of(mockHistory)) };

    await TestBed.configureTestingModule({
      imports: [PatientDetailComponent],
      providers: [
        provideTranslateService(),
        provideMockStore({ initialState: {} }),
        provideCharts(withDefaultRegisterables()),
        { provide: PatientService, useValue: patientServiceMock },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => patientId } } }
        }
      ]
    }).compileComponents();

    useEnglishTestTranslations(TestBed.inject(TranslateService));

    store = TestBed.inject(MockStore);
    store.overrideSelector(selectAllPatients, [mockPatient]);
    store.overrideSelector(selectPatientsLoading, false);

    fixture = TestBed.createComponent(PatientDetailComponent);
    fixture.detectChanges();
  }

  it('defaults to the Overview tab and shows the patient basic info', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('.tab--active')?.textContent).toContain('Overview');
    expect(el.textContent).toContain('Rajesh Kumar');
    expect(el.textContent).toContain('APL-1001');
  });

  it('switches to the Equipment tab and shows device fields', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const tabs = Array.from(el.querySelectorAll('.tab')) as HTMLButtonElement[];
    const equipmentTab = tabs.find((t) => t.textContent?.includes('Equipment'));
    equipmentTab?.click();
    fixture.detectChanges();

    expect(el.querySelector('.tab--active')?.textContent).toContain('Equipment');
    expect(el.textContent).toContain('Medtronic');
    expect(el.textContent).toContain('Evera XT');
    expect(el.textContent).toContain('MDT-ICD-0001');
  });

  it('switches to the History tab and renders the transmission table plus both charts without throwing', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const tabs = Array.from(el.querySelectorAll('.tab')) as HTMLButtonElement[];
    const historyTab = tabs.find((t) => t.textContent?.includes('History'));

    expect(() => {
      historyTab?.click();
      fixture.detectChanges();
    }).not.toThrow();

    expect(el.querySelector('.tab--active')?.textContent).toContain('History');
    expect(el.querySelectorAll('canvas').length).toBe(2);
    expect(el.querySelectorAll('tbody tr').length).toBe(mockHistory.length);
  });

  it('shows the not-found message when the patient id does not match any loaded patient', async () => {
    await setup('999');

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Patient not found.');
  });
});
