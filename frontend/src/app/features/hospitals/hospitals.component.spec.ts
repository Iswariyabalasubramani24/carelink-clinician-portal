import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';

import { Hospital, ProvisionHospitalResult } from '../../core/models/hospital.model';
import { HospitalService } from '../../core/services/hospital.service';
import { useEnglishTestTranslations } from '../../core/testing/translate-testing';
import { HospitalsComponent } from './hospitals.component';

describe('HospitalsComponent', () => {
  let fixture: ComponentFixture<HospitalsComponent>;
  let hospitalServiceMock: {
    getAll: jest.Mock;
    provision: jest.Mock;
  };

  const mockHospitals: Hospital[] = [
    { id: 1, name: 'Apollo Hospital', region: 'India', languageCode: 'en', isActive: true, clinicianCount: 5 },
    { id: 2, name: 'Charite Hospital', region: 'Germany', languageCode: 'de', isActive: false, clinicianCount: 2 }
  ];

  const mockProvisionResult: ProvisionHospitalResult = {
    hospital: { id: 3, name: 'Nihon Medical Center', region: 'Japan', languageCode: 'en', isActive: true, clinicianCount: 1 },
    adminEmail: 'admin@nihonmedical.jp',
    temporaryPassword: 'Tmp#Passw0rd'
  };

  async function setup(options?: { hospitals?: Hospital[]; getAllFails?: boolean }): Promise<void> {
    hospitalServiceMock = {
      getAll: jest.fn().mockReturnValue(
        options?.getAllFails
          ? throwError(() => new Error('network error'))
          : of(options?.hospitals ?? mockHospitals)
      ),
      provision: jest.fn().mockReturnValue(of(mockProvisionResult))
    };

    await TestBed.configureTestingModule({
      imports: [HospitalsComponent],
      providers: [provideTranslateService(), { provide: HospitalService, useValue: hospitalServiceMock }]
    }).compileComponents();

    useEnglishTestTranslations(TestBed.inject(TranslateService));

    fixture = TestBed.createComponent(HospitalsComponent);
    fixture.detectChanges();
  }

  it('renders the hospital list with name, region, clinician count, and status', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const rows = el.querySelectorAll('tbody tr');

    expect(rows.length).toBe(2);
    expect(el.textContent).toContain('Apollo Hospital');
    expect(el.textContent).toContain('India');
    expect(el.textContent).toContain('5');
    expect(el.textContent).toContain('Charite Hospital');
    expect(rows[0].querySelector('.status-badge--active')).toBeTruthy();
    expect(rows[1].querySelector('.status-badge--suspended')).toBeTruthy();
  });

  it('shows the empty state when there are no hospitals', async () => {
    await setup({ hospitals: [] });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('table')).toBeNull();
    expect(el.textContent).toContain('No hospitals yet.');
  });

  it('shows an error message when loading fails', async () => {
    await setup({ getAllFails: true });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('table')).toBeNull();
    expect(el.querySelector('.state-message--error')).toBeTruthy();
  });

  it('opens the provisioning modal from the "+ New Hospital" button', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('.modal-backdrop')).toBeNull();

    (el.querySelector('.hospitals-page__header .btn--primary') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(el.querySelector('.modal-backdrop')).toBeTruthy();
    expect(el.querySelector('#hospitalName')).toBeTruthy();
    expect(el.querySelector('#adminEmail')).toBeTruthy();
  });

  it('submitting the form provisions the hospital and reveals the one-time temp password', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    (el.querySelector('.hospitals-page__header .btn--primary') as HTMLButtonElement).click();
    fixture.detectChanges();

    const type = (selector: string, value: string): void => {
      const input = el.querySelector(selector) as HTMLInputElement;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    };
    type('#hospitalName', 'Nihon Medical Center');
    type('#hospitalRegion', 'Japan');
    type('#adminFirstName', 'Kenji');
    type('#adminLastName', 'Tanaka');
    type('#adminEmail', 'admin@nihonmedical.jp');
    fixture.detectChanges();

    (el.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(hospitalServiceMock.provision).toHaveBeenCalledWith({
      name: 'Nihon Medical Center',
      region: 'Japan',
      languageCode: 'en',
      adminFirstName: 'Kenji',
      adminLastName: 'Tanaka',
      adminEmail: 'admin@nihonmedical.jp'
    });

    // Modal closes, temp password banner appears exactly once with the plaintext.
    expect(el.querySelector('.modal-backdrop')).toBeNull();
    expect(el.querySelector('.temp-password')?.textContent).toContain('Tmp#Passw0rd');
    expect(el.textContent).toContain('admin@nihonmedical.jp');

    // The list reloads to include the new hospital.
    expect(hospitalServiceMock.getAll).toHaveBeenCalledTimes(2);
  });

  it('does not call the service when submitting an invalid (empty) form', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    (el.querySelector('.hospitals-page__header .btn--primary') as HTMLButtonElement).click();
    fixture.detectChanges();

    (el.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(hospitalServiceMock.provision).not.toHaveBeenCalled();
    expect(el.querySelector('.modal-backdrop')).toBeTruthy();
    expect(el.textContent).toContain('Hospital name is required.');
  });
});
