import { By } from '@angular/platform-browser';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Subject } from 'rxjs';

import { CardiacDeviceType, Patient } from '../../../core/models/patient.model';
import { useEnglishTestTranslations } from '../../../core/testing/translate-testing';
import { PatientsActions } from '../../../store/patients/patients.actions';
import { selectPatientCreating } from '../../../store/patients/patients.selectors';
import { AddPatientFormComponent } from './add-patient-form.component';

describe('AddPatientFormComponent', () => {
  let fixture: ComponentFixture<AddPatientFormComponent>;
  let component: AddPatientFormComponent;
  let store: MockStore;
  let actionsSubject: Subject<any>;

  const validValues = {
    medicalRecordNumber: 'APL-1001',
    firstName: 'Rajesh',
    lastName: 'Kumar',
    dateOfBirth: '1965-04-12',
    phoneNumber: '+91-9876543210',
    email: 'rajesh.kumar@example.com',
    deviceType: CardiacDeviceType.ICD,
    deviceManufacturer: 'Medtronic',
    deviceModel: 'Evera XT',
    deviceSerialNumber: 'MDT-ICD-0001',
    implantDate: '2022-03-15'
  };

  function submitForm(): void {
    fixture.debugElement.query(By.css('form')).triggerEventHandler('ngSubmit', null);
    fixture.detectChanges();
  }

  function fieldErrors(): string {
    return fixture.debugElement.nativeElement.textContent as string;
  }

  beforeEach(async () => {
    actionsSubject = new Subject();

    await TestBed.configureTestingModule({
      imports: [AddPatientFormComponent],
      providers: [
        provideTranslateService(),
        provideMockStore({ initialState: {} }),
        provideMockActions(() => actionsSubject.asObservable())
      ]
    }).compileComponents();

    useEnglishTestTranslations(TestBed.inject(TranslateService));

    store = TestBed.inject(MockStore);
    store.overrideSelector(selectPatientCreating, false);

    fixture = TestBed.createComponent(AddPatientFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders all expected fields across both sections', () => {
    const el = fixture.debugElement.nativeElement as HTMLElement;

    expect(el.textContent).toContain('Personal Information');
    expect(el.textContent).toContain('Device Information');

    expect(el.querySelector('#mrn')).toBeTruthy();
    expect(el.querySelector('#firstName')).toBeTruthy();
    expect(el.querySelector('#lastName')).toBeTruthy();
    expect(el.querySelector('#dob')).toBeTruthy();
    expect(el.querySelector('#phone')).toBeTruthy();
    expect(el.querySelector('#email')).toBeTruthy();

    expect(el.querySelector('#deviceType')).toBeTruthy();
    expect(el.querySelector('#manufacturer')).toBeTruthy();
    expect(el.querySelector('#model')).toBeTruthy();
    expect(el.querySelector('#serial')).toBeTruthy();
    expect(el.querySelector('#implantDate')).toBeTruthy();
  });

  it('shows validation errors when required fields are empty', () => {
    submitForm();

    const text = fieldErrors();
    expect(text).toContain('Medical record number is required.');
    expect(text).toContain('First name is required.');
    expect(text).toContain('Last name is required.');
    expect(text).toContain('Date of birth is required.');
    expect(text).toContain('Device type is required.');
    expect(text).toContain('Serial number is required.');
    expect(text).toContain('Implant date is required.');
  });

  it('shows an error for invalid email format', () => {
    component.form.setValue({ ...validValues, email: 'not-an-email' });
    submitForm();

    expect(fieldErrors()).toContain('Enter a valid email address.');
  });

  it('shows an error when implant date precedes date of birth', () => {
    component.form.setValue({
      ...validValues,
      dateOfBirth: '2000-01-01',
      implantDate: '1990-01-01'
    });
    submitForm();

    expect(fieldErrors()).toContain('Implant date cannot be before date of birth.');
  });

  it('dispatches createPatient with the correct payload when valid and submitted', () => {
    jest.spyOn(store, 'dispatch');

    component.form.setValue(validValues);
    submitForm();

    expect(store.dispatch).toHaveBeenCalledWith(
      PatientsActions.createPatient({
        patient: {
          medicalRecordNumber: validValues.medicalRecordNumber,
          firstName: validValues.firstName,
          lastName: validValues.lastName,
          dateOfBirth: validValues.dateOfBirth,
          phoneNumber: validValues.phoneNumber,
          email: validValues.email,
          deviceType: validValues.deviceType,
          deviceManufacturer: validValues.deviceManufacturer,
          deviceModel: validValues.deviceModel,
          deviceSerialNumber: validValues.deviceSerialNumber,
          implantDate: validValues.implantDate
        }
      })
    );
  });

  it('does not dispatch createPatient when the form is invalid', () => {
    jest.spyOn(store, 'dispatch');

    submitForm();

    expect(store.dispatch).not.toHaveBeenCalled();
  });

  it('shows a success message and emits close when creation succeeds', (done) => {
    const closeSpy = jest.fn();
    component.close.subscribe(closeSpy);

    const createdPatient: Patient = {
      id: 1,
      tenantId: 1,
      ...validValues,
      createdAt: '2026-07-18T00:00:00Z'
    };

    actionsSubject.next(PatientsActions.createPatientSuccess({ patient: createdPatient }));
    fixture.detectChanges();

    expect(component.submitSucceeded).toBe(true);
    expect(fieldErrors()).toContain('Patient added successfully.');

    setTimeout(() => {
      expect(closeSpy).toHaveBeenCalled();
      done();
    }, 1200);
  });
});
