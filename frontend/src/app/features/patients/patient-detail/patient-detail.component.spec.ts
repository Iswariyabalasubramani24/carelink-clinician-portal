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

import { Alert, AlertType, AlertUrgency, PatientAlertSetting } from '../../../core/models/alert.model';
import { CardiacDeviceType, Patient } from '../../../core/models/patient.model';
import { TransmissionHistoryPoint } from '../../../core/models/transmission-history.model';
import { AlertService } from '../../../core/services/alert.service';
import { PatientService } from '../../../core/services/patient.service';
import { useEnglishTestTranslations } from '../../../core/testing/translate-testing';
import { selectAllPatients, selectPatientsLoading } from '../../../store/patients/patients.selectors';
import { PatientDetailComponent } from './patient-detail.component';

describe('PatientDetailComponent', () => {
  let fixture: ComponentFixture<PatientDetailComponent>;
  let store: MockStore;
  let patientServiceMock: {
    getTransmissionHistory: jest.Mock;
    getAlerts: jest.Mock;
    getAlertSettings: jest.Mock;
    updateAlertSettings: jest.Mock;
  };
  let alertServiceMock: { acknowledge: jest.Mock; snooze: jest.Mock };

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

  const mockAlert: Alert = {
    id: 10,
    patientId: 1,
    tenantId: 1,
    alertType: AlertType.DisconnectedMonitor,
    urgency: AlertUrgency.Red,
    triggeredAt: '2026-07-20T23:02:49Z',
    isAcknowledged: false
  };

  const mockAlertSettings: PatientAlertSetting[] = [
    { alertType: AlertType.IrregularHeartbeat, effectiveUrgency: AlertUrgency.Yellow, isOverride: false },
    { alertType: AlertType.LowBattery, effectiveUrgency: AlertUrgency.Yellow, isOverride: false },
    { alertType: AlertType.DisconnectedMonitor, effectiveUrgency: AlertUrgency.Red, isOverride: false }
  ];

  async function setup(options?: { alerts?: Alert[]; alertSettings?: PatientAlertSetting[] }): Promise<void> {
    patientServiceMock = {
      getTransmissionHistory: jest.fn().mockReturnValue(of(mockHistory)),
      getAlerts: jest.fn().mockReturnValue(of(options?.alerts ?? [mockAlert])),
      getAlertSettings: jest.fn().mockReturnValue(of(options?.alertSettings ?? mockAlertSettings)),
      updateAlertSettings: jest.fn().mockReturnValue(of(mockAlertSettings))
    };
    alertServiceMock = {
      acknowledge: jest.fn().mockReturnValue(of({ ...mockAlert, isAcknowledged: true })),
      snooze: jest.fn().mockReturnValue(of({ ...mockAlert, snoozedUntil: '2026-08-04T00:00:00Z' }))
    };

    await TestBed.configureTestingModule({
      imports: [PatientDetailComponent],
      providers: [
        provideTranslateService(),
        provideMockStore({ initialState: {} }),
        provideCharts(withDefaultRegisterables()),
        { provide: PatientService, useValue: patientServiceMock },
        { provide: AlertService, useValue: alertServiceMock },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => '1' } } }
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

  function getTabs(): HTMLButtonElement[] {
    return Array.from(fixture.debugElement.nativeElement.querySelectorAll('.tab'));
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
    const equipmentTab = getTabs().find((t) => t.textContent?.includes('Equipment'));
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
    const historyTab = getTabs().find((t) => t.textContent?.includes('History'));

    expect(() => {
      historyTab?.click();
      fixture.detectChanges();
    }).not.toThrow();

    expect(el.querySelector('.tab--active')?.textContent).toContain('History');
    expect(el.querySelectorAll('canvas').length).toBe(2);
    expect(el.querySelectorAll('tbody tr').length).toBe(mockHistory.length);
  });

  it('shows the not-found message when the patient id does not match any loaded patient', async () => {
    patientServiceMock = {
      getTransmissionHistory: jest.fn().mockReturnValue(of(mockHistory)),
      getAlerts: jest.fn().mockReturnValue(of([])),
      getAlertSettings: jest.fn().mockReturnValue(of(mockAlertSettings)),
      updateAlertSettings: jest.fn().mockReturnValue(of(mockAlertSettings))
    };
    alertServiceMock = { acknowledge: jest.fn(), snooze: jest.fn() };

    await TestBed.configureTestingModule({
      imports: [PatientDetailComponent],
      providers: [
        provideTranslateService(),
        provideMockStore({ initialState: {} }),
        provideCharts(withDefaultRegisterables()),
        { provide: PatientService, useValue: patientServiceMock },
        { provide: AlertService, useValue: alertServiceMock },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => '999' } } } }
      ]
    }).compileComponents();

    useEnglishTestTranslations(TestBed.inject(TranslateService));
    store = TestBed.inject(MockStore);
    store.overrideSelector(selectAllPatients, [mockPatient]);
    store.overrideSelector(selectPatientsLoading, false);

    fixture = TestBed.createComponent(PatientDetailComponent);
    fixture.detectChanges();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Patient not found.');
  });

  it('shows a contextual banner on the Overview tab when the patient has an active alert', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const banner = el.querySelector('.alert-banner');
    expect(banner).not.toBeNull();
    expect(banner?.textContent).toContain('Notice of Disconnected Monitor');
    expect(banner?.textContent).toContain('Acknowledge');
    expect(banner?.textContent).toContain('Snooze (15 days)');
  });

  it('does not show a banner when the patient has no active alerts', async () => {
    await setup({ alerts: [] });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('.alert-banner')).toBeNull();
  });

  it('acknowledging an alert calls the service and the banner disappears', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const acknowledgeBtn = Array.from(el.querySelectorAll('.alert-banner__actions button')).find((b) =>
      b.textContent?.includes('Acknowledge')
    ) as HTMLButtonElement;

    acknowledgeBtn.click();
    fixture.detectChanges();

    expect(alertServiceMock.acknowledge).toHaveBeenCalledWith(10);
    expect(el.querySelector('.alert-banner')).toBeNull();
  });

  it('snoozing an alert calls the service and the banner disappears', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const snoozeBtn = Array.from(el.querySelectorAll('.alert-banner__actions button')).find((b) =>
      b.textContent?.includes('Snooze')
    ) as HTMLButtonElement;

    snoozeBtn.click();
    fixture.detectChanges();

    expect(alertServiceMock.snooze).toHaveBeenCalledWith(10);
    expect(el.querySelector('.alert-banner')).toBeNull();
  });

  it('renders the CareAlert Notification tab with clinic-default badges and toggles into override mode', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const careAlertTab = getTabs().find((t) => t.textContent?.includes('CareAlert'));
    careAlertTab?.click();
    fixture.detectChanges();

    expect(el.querySelector('.tab--active')?.textContent).toContain('CareAlert');
    expect(el.querySelectorAll('.care-alert-table tbody tr').length).toBe(3);
    expect(el.querySelectorAll('.urgency-badge').length).toBe(3);
    expect(el.querySelectorAll('.urgency-select').length).toBe(0);

    const overrideToggle = Array.from(el.querySelectorAll('.toggle-option')).find((b) =>
      b.textContent?.includes('Override for this patient')
    ) as HTMLButtonElement;
    overrideToggle.click();
    fixture.detectChanges();

    expect(el.querySelectorAll('.urgency-select').length).toBe(3);
    expect(el.querySelectorAll('.urgency-badge').length).toBe(0);
  });

  it('saves the override selections and shows a success message', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const careAlertTab = getTabs().find((t) => t.textContent?.includes('CareAlert'));
    careAlertTab?.click();
    fixture.detectChanges();

    const overrideToggle = Array.from(el.querySelectorAll('.toggle-option')).find((b) =>
      b.textContent?.includes('Override for this patient')
    ) as HTMLButtonElement;
    overrideToggle.click();
    fixture.detectChanges();

    const saveBtn = el.querySelector('.care-alert-actions button') as HTMLButtonElement;
    saveBtn.click();
    fixture.detectChanges();

    expect(patientServiceMock.updateAlertSettings).toHaveBeenCalledWith(
      1,
      expect.objectContaining({ useOverride: true, overrides: expect.any(Array) })
    );
    expect(el.textContent).toContain('Saved.');
  });
});
