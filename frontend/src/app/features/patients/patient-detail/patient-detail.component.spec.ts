import 'jest-canvas-mock';

// jsdom has no ResizeObserver, but Chart.js's `responsive: true` option needs
// one to attach its resize listener - stub it so chart construction doesn't throw.
class ResizeObserverMock {
  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
}
(globalThis as unknown as { ResizeObserver: unknown }).ResizeObserver = ResizeObserverMock;

// jsdom doesn't implement the Blob URL APIs used to trigger a PDF download.
window.URL.createObjectURL = jest.fn().mockReturnValue('blob:mock-url');
window.URL.revokeObjectURL = jest.fn();

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { of } from 'rxjs';

import { Alert, AlertType, AlertUrgency, PatientAlertSetting } from '../../../core/models/alert.model';
import { PatientNote } from '../../../core/models/patient-note.model';
import { CardiacDeviceType, Patient } from '../../../core/models/patient.model';
import { PatientReportSettings, Report, ReportType } from '../../../core/models/report.model';
import { PatientScheduleSettings } from '../../../core/models/schedule.model';
import { TransmissionHistoryPoint } from '../../../core/models/transmission-history.model';
import { AlertService } from '../../../core/services/alert.service';
import { PatientService } from '../../../core/services/patient.service';
import { ReportService } from '../../../core/services/report.service';
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
    getReports: jest.Mock;
    generateReport: jest.Mock;
    getReportSettings: jest.Mock;
    updateReportSettings: jest.Mock;
    getScheduleSettings: jest.Mock;
    updateScheduleSettings: jest.Mock;
    getNotes: jest.Mock;
    createNote: jest.Mock;
  };
  let alertServiceMock: { acknowledge: jest.Mock; snooze: jest.Mock };
  let reportServiceMock: { download: jest.Mock };

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

  const mockReports: Report[] = [
    { id: 1, patientId: 1, reportType: ReportType.FullReport, generatedAt: '2026-07-01T10:00:00Z' }
  ];

  const mockReportSettings: PatientReportSettings = { intervalDays: 30, isOverride: false };

  const mockScheduleSettings: PatientScheduleSettings = { intervalDays: 30, isOverride: false };

  const mockNotes: PatientNote[] = [
    { id: 1, patientId: 1, clinicianName: 'Anita Rao', content: 'Patient is stable.', createdAt: '2026-07-20T10:00:00Z' }
  ];

  async function setup(options?: {
    alerts?: Alert[];
    alertSettings?: PatientAlertSetting[];
    reports?: Report[];
    reportSettings?: PatientReportSettings;
    scheduleSettings?: PatientScheduleSettings;
    notes?: PatientNote[];
  }): Promise<void> {
    patientServiceMock = {
      getTransmissionHistory: jest.fn().mockReturnValue(of(mockHistory)),
      getAlerts: jest.fn().mockReturnValue(of(options?.alerts ?? [mockAlert])),
      getAlertSettings: jest.fn().mockReturnValue(of(options?.alertSettings ?? mockAlertSettings)),
      updateAlertSettings: jest.fn().mockReturnValue(of(mockAlertSettings)),
      getReports: jest.fn().mockReturnValue(of(options?.reports ?? mockReports)),
      generateReport: jest.fn().mockReturnValue(of(mockReports[0])),
      getReportSettings: jest.fn().mockReturnValue(of(options?.reportSettings ?? mockReportSettings)),
      updateReportSettings: jest.fn().mockReturnValue(of(mockReportSettings)),
      getScheduleSettings: jest.fn().mockReturnValue(of(options?.scheduleSettings ?? mockScheduleSettings)),
      updateScheduleSettings: jest.fn().mockReturnValue(of(mockScheduleSettings)),
      getNotes: jest.fn().mockReturnValue(of(options?.notes ?? mockNotes)),
      createNote: jest.fn().mockReturnValue(
        of({ id: 2, patientId: 1, clinicianName: 'Anita Rao', content: 'New note content', createdAt: '2026-07-21T00:00:00Z' })
      )
    };
    alertServiceMock = {
      acknowledge: jest.fn().mockReturnValue(of({ ...mockAlert, isAcknowledged: true })),
      snooze: jest.fn().mockReturnValue(of({ ...mockAlert, snoozedUntil: '2026-08-04T00:00:00Z' }))
    };
    reportServiceMock = {
      download: jest.fn().mockReturnValue(of(new Blob(['pdf-bytes'], { type: 'application/pdf' })))
    };

    await TestBed.configureTestingModule({
      imports: [PatientDetailComponent],
      providers: [
        provideTranslateService(),
        provideMockStore({ initialState: {} }),
        provideCharts(withDefaultRegisterables()),
        { provide: PatientService, useValue: patientServiceMock },
        { provide: AlertService, useValue: alertServiceMock },
        { provide: ReportService, useValue: reportServiceMock },
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

  it('switches to the Profile tab and shows read-only patient identity fields', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const profileTab = getTabs().find((t) => t.textContent?.trim() === 'Profile');
    profileTab?.click();
    fixture.detectChanges();

    expect(el.querySelector('.tab--active')?.textContent).toContain('Profile');
    expect(el.textContent).toContain('Rajesh');
    expect(el.textContent).toContain('Kumar');
    expect(el.textContent).toContain('APL-1001');
    expect(el.textContent).toContain('Active');
  });

  it('renders the Schedule tab with the clinic-default interval and a read-only input', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const scheduleTab = getTabs().find((t) => t.textContent?.trim() === 'Schedule');
    scheduleTab?.click();
    fixture.detectChanges();

    expect(el.querySelector('.tab--active')?.textContent).toContain('Schedule');
    const toggle = Array.from(el.querySelectorAll('.toggle-option')).find((b) =>
      b.textContent?.includes('Use clinic default')
    ) as HTMLButtonElement;
    expect(toggle.classList.contains('toggle-option--active')).toBe(true);

    const intervalInput = el.querySelector('#scheduleIntervalDays') as HTMLInputElement;
    expect(intervalInput.value).toBe('30');
    expect(intervalInput.readOnly).toBe(true);
  });

  it('renders the Schedule tab already in override mode when the patient has a per-patient override', async () => {
    await setup({ scheduleSettings: { intervalDays: 14, isOverride: true } });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const scheduleTab = getTabs().find((t) => t.textContent?.trim() === 'Schedule');
    scheduleTab?.click();
    fixture.detectChanges();

    const overrideToggle = Array.from(el.querySelectorAll('.toggle-option')).find((b) =>
      b.textContent?.includes('Override for this patient')
    ) as HTMLButtonElement;
    expect(overrideToggle.classList.contains('toggle-option--active')).toBe(true);

    const intervalInput = el.querySelector('#scheduleIntervalDays') as HTMLInputElement;
    expect(intervalInput.value).toBe('14');
    expect(intervalInput.readOnly).toBe(false);
  });

  it('toggling to override, changing the interval, and saving persists the override', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const scheduleTab = getTabs().find((t) => t.textContent?.trim() === 'Schedule');
    scheduleTab?.click();
    fixture.detectChanges();

    const overrideToggle = Array.from(el.querySelectorAll('.toggle-option')).find((b) =>
      b.textContent?.includes('Override for this patient')
    ) as HTMLButtonElement;
    overrideToggle.click();
    fixture.detectChanges();

    const intervalInput = el.querySelector('#scheduleIntervalDays') as HTMLInputElement;
    intervalInput.value = '14';
    intervalInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const saveBtn = Array.from(el.querySelectorAll('.care-alert-actions button')).find((b) =>
      b.textContent?.includes('Save')
    ) as HTMLButtonElement;
    saveBtn.click();
    fixture.detectChanges();

    expect(patientServiceMock.updateScheduleSettings).toHaveBeenCalledWith(
      1,
      expect.objectContaining({ useOverride: true, intervalDays: 14 })
    );
    expect(el.textContent).toContain('Saved.');
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
      updateAlertSettings: jest.fn().mockReturnValue(of(mockAlertSettings)),
      getReports: jest.fn().mockReturnValue(of(mockReports)),
      generateReport: jest.fn().mockReturnValue(of(mockReports[0])),
      getReportSettings: jest.fn().mockReturnValue(of(mockReportSettings)),
      updateReportSettings: jest.fn().mockReturnValue(of(mockReportSettings)),
      getScheduleSettings: jest.fn().mockReturnValue(of(mockScheduleSettings)),
      updateScheduleSettings: jest.fn().mockReturnValue(of(mockScheduleSettings)),
      getNotes: jest.fn().mockReturnValue(of(mockNotes)),
      createNote: jest.fn().mockReturnValue(of(mockNotes[0]))
    };
    alertServiceMock = { acknowledge: jest.fn(), snooze: jest.fn() };
    reportServiceMock = { download: jest.fn() };

    await TestBed.configureTestingModule({
      imports: [PatientDetailComponent],
      providers: [
        provideTranslateService(),
        provideMockStore({ initialState: {} }),
        provideCharts(withDefaultRegisterables()),
        { provide: PatientService, useValue: patientServiceMock },
        { provide: AlertService, useValue: alertServiceMock },
        { provide: ReportService, useValue: reportServiceMock },
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

  it('renders the Reports tab with the generated reports list', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const reportsTab = getTabs().find((t) => t.textContent?.includes('Reports'));
    reportsTab?.click();
    fixture.detectChanges();

    expect(el.querySelector('.tab--active')?.textContent).toContain('Reports');
    expect(el.querySelectorAll('.history-table tbody tr').length).toBe(mockReports.length);
    expect(el.textContent).toContain('Full Report');
  });

  it('shows the empty-state message when the patient has no reports yet', async () => {
    await setup({ reports: [] });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const reportsTab = getTabs().find((t) => t.textContent?.includes('Reports'));
    reportsTab?.click();
    fixture.detectChanges();

    expect(el.textContent).toContain('No reports generated yet.');
  });

  it('clicking Generate Report calls the service with the selected type and reloads the list', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const reportsTab = getTabs().find((t) => t.textContent?.includes('Reports'));
    reportsTab?.click();
    fixture.detectChanges();

    const generateBtn = Array.from(el.querySelectorAll('.report-generate button')).find((b) =>
      b.textContent?.includes('Generate Report')
    ) as HTMLButtonElement;
    generateBtn.click();
    fixture.detectChanges();

    expect(patientServiceMock.generateReport).toHaveBeenCalledWith(1, ReportType.FullReport);
    expect(patientServiceMock.getReports).toHaveBeenCalledTimes(2);
  });

  it('the download button is present per report row and triggers the report service with the correct id', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const reportsTab = getTabs().find((t) => t.textContent?.includes('Reports'));
    reportsTab?.click();
    fixture.detectChanges();

    const downloadBtn = Array.from(el.querySelectorAll('.history-table tbody button')).find((b) =>
      b.textContent?.includes('Download')
    ) as HTMLButtonElement;
    expect(downloadBtn).toBeTruthy();

    downloadBtn.click();
    fixture.detectChanges();

    expect(reportServiceMock.download).toHaveBeenCalledWith(mockReports[0].id);
  });

  it('renders the Comments and Notes tab with the existing notes list', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const notesTab = getTabs().find((t) => t.textContent?.includes('Comments and Notes'));
    notesTab?.click();
    fixture.detectChanges();

    expect(el.querySelector('.tab--active')?.textContent).toContain('Comments and Notes');
    expect(el.querySelectorAll('.note-item').length).toBe(mockNotes.length);
    expect(el.textContent).toContain('Anita Rao');
    expect(el.textContent).toContain('Patient is stable.');
  });

  it('shows the empty-state message when the patient has no notes yet', async () => {
    await setup({ notes: [] });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const notesTab = getTabs().find((t) => t.textContent?.includes('Comments and Notes'));
    notesTab?.click();
    fixture.detectChanges();

    expect(el.textContent).toContain('No notes yet.');
  });

  it('clicking Post adds the new note to the top of the list and clears the textarea', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const notesTab = getTabs().find((t) => t.textContent?.includes('Comments and Notes'));
    notesTab?.click();
    fixture.detectChanges();

    const textarea = el.querySelector('.note-textarea') as HTMLTextAreaElement;
    textarea.value = 'New note content';
    textarea.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const postBtn = Array.from(el.querySelectorAll('.note-composer__actions button')).find((b) =>
      b.textContent?.includes('Post')
    ) as HTMLButtonElement;
    postBtn.click();
    fixture.detectChanges();

    expect(patientServiceMock.createNote).toHaveBeenCalledWith(1, 'New note content');
    expect(el.querySelectorAll('.note-item').length).toBe(mockNotes.length + 1);
    expect(el.textContent).toContain('New note content');
    expect(textarea.value).toBe('');
  });
});
