import { By } from '@angular/platform-browser';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { Subject } from 'rxjs';

import { CardiacDeviceType, Patient } from '../../core/models/patient.model';
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
      createdAt: '2026-07-17T20:34:55Z'
    }
  ];

  async function setup(patients: Patient[]): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [PatientsListComponent],
      providers: [
        provideMockStore({ initialState: {} }),
        provideMockActions(() => new Subject().asObservable())
      ]
    }).compileComponents();

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
});
