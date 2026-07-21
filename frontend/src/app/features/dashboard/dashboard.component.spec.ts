import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';

import { AlertType, AlertUrgency, Alert } from '../../core/models/alert.model';
import { Clinician } from '../../core/models/auth.model';
import { DashboardSummary } from '../../core/models/dashboard-summary.model';
import { CardiacDeviceType, Patient } from '../../core/models/patient.model';
import { TransmissionScheduleEntry } from '../../core/models/transmission-schedule.model';
import { AlertService } from '../../core/services/alert.service';
import { AuthService } from '../../core/services/auth.service';
import { DashboardService } from '../../core/services/dashboard.service';
import { PatientService } from '../../core/services/patient.service';
import { ScheduleService } from '../../core/services/schedule.service';
import { useEnglishTestTranslations } from '../../core/testing/translate-testing';
import { DashboardComponent } from './dashboard.component';

describe('DashboardComponent', () => {
  let fixture: ComponentFixture<DashboardComponent>;
  let dashboardServiceMock: { getSummary: jest.Mock };
  let alertServiceMock: { getActive: jest.Mock };
  let scheduleServiceMock: { getTransmissionSchedule: jest.Mock };
  let patientServiceMock: { getAll: jest.Mock };
  let authServiceMock: { getCurrentClinician: jest.Mock };

  const summary: DashboardSummary = {
    newPatientsCount: 3,
    disconnectedMonitorsCount: 2,
    totalActivePatientsCount: 17,
    activeAlertsCount: 24
  };

  const mockPatients: Patient[] = [
    {
      id: 1,
      tenantId: 1,
      medicalRecordNumber: 'APL-1001',
      firstName: 'Rajesh',
      lastName: 'Kumar',
      dateOfBirth: '1965-04-12',
      deviceType: CardiacDeviceType.ICD,
      deviceSerialNumber: 'MDT-ICD-0001',
      implantDate: '2022-03-15',
      isActive: true,
      createdAt: '2026-01-01T00:00:00Z'
    }
  ];

  const mockAlerts: Alert[] = [
    {
      id: 10,
      patientId: 1,
      tenantId: 1,
      alertType: AlertType.LowBattery,
      urgency: AlertUrgency.Yellow,
      triggeredAt: '2026-07-20T10:00:00Z',
      isAcknowledged: false
    }
  ];

  const mockTransmissions: TransmissionScheduleEntry[] = [
    {
      patientId: 1,
      patientName: 'Rajesh Kumar',
      lastSyncedAt: '2026-07-01T09:00:00Z',
      intervalDays: 30,
      nextScheduledDate: '2026-07-31T09:00:00Z'
    }
  ];

  const clinicianAdmin: Clinician = {
    id: 2,
    email: 'admin@apollo.com',
    firstName: 'Meera',
    lastName: 'Pillai',
    role: 'Admin',
    tenantId: 1
  };

  const clinicianNonAdmin: Clinician = {
    id: 1,
    email: 'doctor@apollo.com',
    firstName: 'Anita',
    lastName: 'Rao',
    role: 'Clinician',
    tenantId: 1
  };

  async function setup(options?: {
    getSummaryReturn?: Observable<DashboardSummary>;
    alerts?: Alert[];
    transmissions?: TransmissionScheduleEntry[];
    clinician?: Clinician;
  }): Promise<void> {
    dashboardServiceMock = {
      getSummary: jest.fn().mockReturnValue(options?.getSummaryReturn ?? of(summary))
    };
    alertServiceMock = { getActive: jest.fn().mockReturnValue(of(options?.alerts ?? mockAlerts)) };
    scheduleServiceMock = {
      getTransmissionSchedule: jest.fn().mockReturnValue(of(options?.transmissions ?? mockTransmissions))
    };
    patientServiceMock = { getAll: jest.fn().mockReturnValue(of(mockPatients)) };
    authServiceMock = { getCurrentClinician: jest.fn().mockReturnValue(options?.clinician ?? clinicianNonAdmin) };

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideTranslateService(),
        { provide: DashboardService, useValue: dashboardServiceMock },
        { provide: AlertService, useValue: alertServiceMock },
        { provide: ScheduleService, useValue: scheduleServiceMock },
        { provide: PatientService, useValue: patientServiceMock },
        { provide: AuthService, useValue: authServiceMock },
        provideRouter([])
      ]
    }).compileComponents();

    useEnglishTestTranslations(TestBed.inject(TranslateService));

    fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
  }

  it('renders the four widget cards with the counts returned by the summary endpoint', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const counts = Array.from(el.querySelectorAll('.widget-card__count')).map((n) => n.textContent?.trim());

    expect(counts).toEqual(['3', '2', '17', '24']);
    expect(el.textContent).toContain('New Patients This Week');
    expect(el.textContent).toContain('Disconnected Monitors');
    expect(el.textContent).toContain('Total Active Patients');
    expect(el.textContent).toContain('Active Alerts');
  });

  it('navigates to /patients when a widget card is clicked', async () => {
    await setup();

    const router = TestBed.inject(Router);
    const navigateSpy = jest.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    const firstWidget: HTMLButtonElement = fixture.debugElement.nativeElement.querySelector('.widget-card');
    firstWidget.click();

    expect(navigateSpy).toHaveBeenCalledWith('/patients');
  });

  it('shows an error message instead of widgets when the summary request fails', async () => {
    await setup({ getSummaryReturn: throwError(() => new Error('network error')) });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('.widget-card')).toBeNull();
    expect(el.textContent).toContain('Failed to load dashboard summary.');
  });

  it('shows Patients and Transmission Schedule quick links to a regular clinician, but not admin-only links', async () => {
    await setup({ clinician: clinicianNonAdmin });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('.quick-link')).map((l) => l.textContent?.trim());

    expect(links.some((l) => l?.includes('Patients'))).toBe(true);
    expect(links.some((l) => l?.includes('Transmission Schedule'))).toBe(true);
    expect(links.some((l) => l?.includes('Clinic Management'))).toBe(false);
    expect(links.some((l) => l?.includes('Audit Log'))).toBe(false);
  });

  it('shows the admin-only quick links to an Admin clinician', async () => {
    await setup({ clinician: clinicianAdmin });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('.quick-link')).map((l) => l.textContent?.trim());

    expect(links.some((l) => l?.includes('Clinic Management'))).toBe(true);
    expect(links.some((l) => l?.includes('Audit Log'))).toBe(true);
  });

  it('renders recent alerts with the patient name resolved from the patient list', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const items = el.querySelectorAll('.alert-list__item');

    expect(items.length).toBe(1);
    expect(el.textContent).toContain('Rajesh Kumar');
    expect(el.textContent).toContain('Low Battery');
  });

  it('shows the empty state when there are no active alerts', async () => {
    await setup({ alerts: [] });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('.alert-list__item')).toBeNull();
    expect(el.textContent).toContain('No active alerts.');
  });

  it('renders upcoming transmissions with a link to the full schedule', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const items = el.querySelectorAll('.transmission-list__item');

    expect(items.length).toBe(1);
    expect(el.textContent).toContain('Rajesh Kumar');
    expect(el.textContent).toContain('View All');
  });

  it('shows the empty state when there are no upcoming transmissions', async () => {
    await setup({ transmissions: [] });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('.transmission-list__item')).toBeNull();
    expect(el.textContent).toContain('No upcoming transmissions.');
  });

  it('excludes never-synced patients (null next scheduled date) from upcoming transmissions', async () => {
    await setup({
      transmissions: [
        ...mockTransmissions,
        { patientId: 2, patientName: 'Never Synced', lastSyncedAt: null, intervalDays: 30, nextScheduledDate: null }
      ]
    });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const items = el.querySelectorAll('.transmission-list__item');

    expect(items.length).toBe(1);
    expect(el.textContent).not.toContain('Never Synced');
  });
});
