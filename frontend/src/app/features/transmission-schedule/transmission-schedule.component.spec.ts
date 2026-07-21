import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';

import { TransmissionScheduleEntry } from '../../core/models/transmission-schedule.model';
import { ScheduleService } from '../../core/services/schedule.service';
import { useEnglishTestTranslations } from '../../core/testing/translate-testing';
import { TransmissionScheduleComponent } from './transmission-schedule.component';

describe('TransmissionScheduleComponent', () => {
  let fixture: ComponentFixture<TransmissionScheduleComponent>;
  let scheduleServiceMock: { getTransmissionSchedule: jest.Mock };

  const mockEntries: TransmissionScheduleEntry[] = [
    {
      patientId: 2,
      patientName: 'Sarah John',
      lastSyncedAt: '2026-06-15T09:00:00Z',
      intervalDays: 14,
      nextScheduledDate: '2026-07-25T09:00:00Z'
    },
    {
      patientId: 1,
      patientName: 'Rajesh Kumar',
      lastSyncedAt: '2026-07-01T09:00:00Z',
      intervalDays: 30,
      nextScheduledDate: '2026-07-31T09:00:00Z'
    },
    {
      patientId: 3,
      patientName: 'Never Synced',
      lastSyncedAt: null,
      intervalDays: 30,
      nextScheduledDate: null
    }
  ];

  async function setup(options?: { getTransmissionScheduleReturn?: Observable<TransmissionScheduleEntry[]> }): Promise<void> {
    scheduleServiceMock = {
      getTransmissionSchedule: jest.fn().mockReturnValue(options?.getTransmissionScheduleReturn ?? of(mockEntries))
    };

    await TestBed.configureTestingModule({
      imports: [TransmissionScheduleComponent],
      providers: [provideTranslateService(), { provide: ScheduleService, useValue: scheduleServiceMock }]
    }).compileComponents();

    useEnglishTestTranslations(TestBed.inject(TranslateService));

    fixture = TestBed.createComponent(TransmissionScheduleComponent);
    fixture.detectChanges();
  }

  it('shows a loading message while the request is in flight', async () => {
    scheduleServiceMock = { getTransmissionSchedule: jest.fn().mockReturnValue(new Observable<TransmissionScheduleEntry[]>()) };

    await TestBed.configureTestingModule({
      imports: [TransmissionScheduleComponent],
      providers: [provideTranslateService(), { provide: ScheduleService, useValue: scheduleServiceMock }]
    }).compileComponents();

    useEnglishTestTranslations(TestBed.inject(TranslateService));

    fixture = TestBed.createComponent(TransmissionScheduleComponent);
    fixture.detectChanges();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('table')).toBeNull();
    expect(el.textContent).toContain('Loading');
  });

  it('renders every entry with patient, last synced, interval, and next scheduled columns', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const rows = el.querySelectorAll('tbody tr');

    expect(rows.length).toBe(3);
    expect(el.textContent).toContain('Rajesh Kumar');
    expect(el.textContent).toContain('30');
    expect(el.textContent).toContain('Sarah John');
    expect(el.textContent).toContain('14');
    expect(el.textContent).toContain('Never Synced');
    expect(el.textContent).toContain('—');
  });

  it('shows the empty state when there are no entries', async () => {
    await setup({ getTransmissionScheduleReturn: of([]) });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('table')).toBeNull();
    expect(el.textContent).toContain('No patients found.');
  });

  it('shows an error message when the request fails', async () => {
    await setup({ getTransmissionScheduleReturn: throwError(() => new Error('network error')) });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('table')).toBeNull();
    expect(el.querySelector('.state-message--error')).toBeTruthy();
  });

  it('toggles sort direction on the Next Scheduled header, moving never-synced patients last in both directions', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const header = el.querySelector('.sortable-header') as HTMLElement;

    expect(header.querySelector('.sort-indicator')?.textContent).toContain('▲');
    let names = Array.from(el.querySelectorAll('.patient-name')).map((n) => n.textContent?.trim());
    expect(names).toEqual(['Sarah John', 'Rajesh Kumar', 'Never Synced']);

    header.click();
    fixture.detectChanges();

    expect(header.querySelector('.sort-indicator')?.textContent).toContain('▼');
    names = Array.from(el.querySelectorAll('.patient-name')).map((n) => n.textContent?.trim());
    expect(names).toEqual(['Rajesh Kumar', 'Sarah John', 'Never Synced']);

    header.click();
    fixture.detectChanges();

    expect(header.querySelector('.sort-indicator')?.textContent).toContain('▲');
    names = Array.from(el.querySelectorAll('.patient-name')).map((n) => n.textContent?.trim());
    expect(names).toEqual(['Sarah John', 'Rajesh Kumar', 'Never Synced']);
  });
});
