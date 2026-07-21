import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';

import { PagedAuditLogs } from '../../core/models/audit-log.model';
import { AuditLogService } from '../../core/services/audit-log.service';
import { useEnglishTestTranslations } from '../../core/testing/translate-testing';
import { AuditLogComponent } from './audit-log.component';

describe('AuditLogComponent', () => {
  let fixture: ComponentFixture<AuditLogComponent>;
  let auditLogServiceMock: { getAll: jest.Mock };

  const mockPage: PagedAuditLogs = {
    items: [
      {
        id: 1,
        clinicianId: 7,
        action: 'PatientCreated',
        entityType: 'Patient',
        entityId: 42,
        timestamp: '2026-07-21T10:00:00Z',
        details: 'MRN APL-1001'
      },
      {
        id: 2,
        clinicianId: 3,
        action: 'AlertAcknowledged',
        entityType: 'Alert',
        entityId: 5,
        timestamp: '2026-07-20T09:30:00Z',
        details: 'LowBattery for patient 1'
      }
    ],
    totalCount: 2,
    page: 1,
    pageSize: 25
  };

  async function setup(page: PagedAuditLogs | null = mockPage): Promise<void> {
    auditLogServiceMock = {
      getAll: jest.fn().mockReturnValue(page ? of(page) : throwError(() => new Error('failed')))
    };

    await TestBed.configureTestingModule({
      imports: [AuditLogComponent],
      providers: [provideTranslateService(), { provide: AuditLogService, useValue: auditLogServiceMock }]
    }).compileComponents();

    useEnglishTestTranslations(TestBed.inject(TranslateService));

    fixture = TestBed.createComponent(AuditLogComponent);
    fixture.detectChanges();
  }

  it('renders audit log entries with action, entity, clinician, and details', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const rows = el.querySelectorAll('tbody tr');

    expect(auditLogServiceMock.getAll).toHaveBeenCalledWith(1, 25, undefined);
    expect(rows.length).toBe(2);
    expect(el.textContent).toContain('Patient Created');
    expect(el.textContent).toContain('Patient #42');
    expect(el.textContent).toContain('MRN APL-1001');
    expect(el.textContent).toContain('Alert Acknowledged');
  });

  it('shows the empty-state message when there are no entries', async () => {
    await setup({ items: [], totalCount: 0, page: 1, pageSize: 25 });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('tbody')).toBeNull();
    expect(el.textContent).toContain('No audit log entries found.');
  });

  it('shows an error message when loading fails', async () => {
    await setup(null);

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Failed to load the audit log.');
  });

  it('filtering by action re-fetches with the selected action and resets to page 1', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const patientCreatedFilter = Array.from(el.querySelectorAll('.toggle-option')).find((b) =>
      b.textContent?.trim() === 'Patient Created'
    ) as HTMLButtonElement;

    patientCreatedFilter.click();
    fixture.detectChanges();

    expect(auditLogServiceMock.getAll).toHaveBeenLastCalledWith(1, 25, 'PatientCreated');
  });

  it('pagination Next requests the following page', async () => {
    await setup({ ...mockPage, totalCount: 60 });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const nextBtn = Array.from(el.querySelectorAll('.pagination button')).find((b) =>
      b.textContent?.trim() === 'Next'
    ) as HTMLButtonElement;

    nextBtn.click();
    fixture.detectChanges();

    expect(auditLogServiceMock.getAll).toHaveBeenLastCalledWith(2, 25, undefined);
  });
});
