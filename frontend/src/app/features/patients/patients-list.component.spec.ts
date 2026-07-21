import { By } from '@angular/platform-browser';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideMockActions } from '@ngrx/effects/testing';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Subject } from 'rxjs';

import { CardiacDeviceType, Patient } from '../../core/models/patient.model';
import { useEnglishTestTranslations } from '../../core/testing/translate-testing';
import { PatientsActions } from '../../store/patients/patients.actions';
import {
  selectAllPatients,
  selectPatientCreating,
  selectPatientsError,
  selectPatientsLoading
} from '../../store/patients/patients.selectors';
import { PatientsListComponent } from './patients-list.component';

describe('PatientsListComponent', () => {
  let fixture: ComponentFixture<PatientsListComponent>;
  let component: PatientsListComponent;
  let store: MockStore;

  const mockPatients: Patient[] = [
    {
      id: 1,
      tenantId: 1,
      medicalRecordNumber: 'APL-1001',
      firstName: 'Paul',
      lastName: 'Joseph',
      dateOfBirth: '1965-04-12',
      deviceType: CardiacDeviceType.ICD,
      deviceSerialNumber: 'MDT-ICD-0001',
      implantDate: '2022-03-15',
      isActive: true,
      createdAt: '2026-07-17T20:34:52Z'
    },
    {
      id: 2,
      tenantId: 1,
      medicalRecordNumber: 'APL-1002',
      firstName: 'Sarah',
      lastName: 'John',
      dateOfBirth: '1972-09-30',
      deviceType: CardiacDeviceType.Pacemaker,
      deviceSerialNumber: 'BSX-PM-0002',
      implantDate: '2021-11-02',
      isActive: true,
      createdAt: '2026-07-17T20:34:55Z'
    }
  ];

  async function setup(patients: Patient[]): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [PatientsListComponent],
      providers: [
        provideTranslateService(),
        provideMockStore({ initialState: {} }),
        provideMockActions(() => new Subject().asObservable()),
        provideRouter([])
      ]
    }).compileComponents();

    useEnglishTestTranslations(TestBed.inject(TranslateService));

    store = TestBed.inject(MockStore);
    store.overrideSelector(selectAllPatients, patients);
    store.overrideSelector(selectPatientsLoading, false);
    store.overrideSelector(selectPatientsError, null);
    store.overrideSelector(selectPatientCreating, false);

    fixture = TestBed.createComponent(PatientsListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('renders the patient list correctly given mock store data', async () => {
    await setup(mockPatients);

    const rows = fixture.debugElement.queryAll(By.css('tbody tr'));
    expect(rows.length).toBe(2);

    const text = (fixture.debugElement.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('APL-1001');
    expect(text).toContain('Paul Joseph');
    expect(text).toContain('ICD');
    expect(text).toContain('MDT-ICD-0001');
    expect(text).toContain('APL-1002');
    expect(text).toContain('Sarah John');
    expect(text).toContain('Pacemaker');
  });

  it('renders the empty state when no patients exist', async () => {
    await setup([]);

    const text = (fixture.debugElement.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('No patients yet');
    expect(fixture.debugElement.query(By.css('table'))).toBeFalsy();
  });

  it('opens the modal when the "+ Add Patient" button is clicked', async () => {
    await setup(mockPatients);

    expect(fixture.debugElement.query(By.css('.modal-backdrop'))).toBeFalsy();

    const addButton = fixture.debugElement.query(By.css('button.btn--primary'));
    addButton.nativeElement.click();
    fixture.detectChanges();

    expect(component.showAddForm).toBe(true);
    expect(fixture.debugElement.query(By.css('.modal-backdrop'))).toBeTruthy();
    expect(fixture.debugElement.query(By.css('app-add-patient-form'))).toBeTruthy();
  });

  it('the Advanced Search panel is collapsed by default and expands on toggle click', async () => {
    await setup(mockPatients);

    expect(fixture.debugElement.query(By.css('.advanced-search__form'))).toBeFalsy();

    const toggle = fixture.debugElement.query(By.css('.advanced-search__toggle'));
    toggle.nativeElement.click();
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('.advanced-search__form'))).toBeTruthy();
  });

  it('submitting the search form dispatches searchPatients with the entered filters', async () => {
    await setup(mockPatients);
    const dispatchSpy = jest.spyOn(store, 'dispatch');

    fixture.debugElement.query(By.css('.advanced-search__toggle')).nativeElement.click();
    fixture.detectChanges();

    const deviceSelect = fixture.debugElement.query(By.css('#searchDeviceType')).nativeElement as HTMLSelectElement;
    deviceSelect.value = CardiacDeviceType.ICD;
    deviceSelect.dispatchEvent(new Event('change'));

    const statusSelect = fixture.debugElement.query(By.css('#searchStatus')).nativeElement as HTMLSelectElement;
    statusSelect.value = 'active';
    statusSelect.dispatchEvent(new Event('change'));

    const keywordInput = fixture.debugElement.query(By.css('#searchKeyword')).nativeElement as HTMLInputElement;
    keywordInput.value = 'Paul';
    keywordInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    fixture.debugElement.query(By.css('.advanced-search__form')).triggerEventHandler('ngSubmit', null);
    fixture.detectChanges();

    expect(dispatchSpy).toHaveBeenCalledWith(
      PatientsActions.searchPatients({
        filters: {
          deviceType: CardiacDeviceType.ICD,
          isActive: true,
          implantDateFrom: undefined,
          implantDateTo: undefined,
          keyword: 'Paul'
        }
      })
    );
  });

  it('clicking Clear Filters resets the form and dispatches an unfiltered loadPatients', async () => {
    await setup(mockPatients);

    fixture.debugElement.query(By.css('.advanced-search__toggle')).nativeElement.click();
    fixture.detectChanges();

    const keywordInput = fixture.debugElement.query(By.css('#searchKeyword')).nativeElement as HTMLInputElement;
    keywordInput.value = 'Paul';
    keywordInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const dispatchSpy = jest.spyOn(store, 'dispatch');
    const clearButton = Array.from(
      fixture.debugElement.queryAll(By.css('.advanced-search__actions button'))
    ).find((b) => b.nativeElement.textContent.includes('Clear Filters'))!;
    clearButton.nativeElement.click();
    fixture.detectChanges();

    expect(keywordInput.value).toBe('');
    expect(dispatchSpy).toHaveBeenCalledWith(PatientsActions.loadPatients());
  });
});
