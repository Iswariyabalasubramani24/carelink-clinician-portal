import { By } from '@angular/platform-browser';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Subject } from 'rxjs';

import { CardiacDeviceType, Patient } from '../../../core/models/patient.model';
import { useEnglishTestTranslations } from '../../../core/testing/translate-testing';
import { PatientsActions } from '../../../store/patients/patients.actions';
import { selectPatientUpdating } from '../../../store/patients/patients.selectors';
import { EditPatientFormComponent } from './edit-patient-form.component';

describe('EditPatientFormComponent', () => {
  let fixture: ComponentFixture<EditPatientFormComponent>;
  let component: EditPatientFormComponent;
  let store: MockStore;
  let actionsSubject: Subject<any>;

  const existingPatient: Patient = {
    id: 1,
    tenantId: 1,
    medicalRecordNumber: 'APL-1001',
    firstName: 'Rajesh',
    lastName: 'Kumar',
    dateOfBirth: '1965-04-12T00:00:00Z',
    phoneNumber: '+91-9876543210',
    email: 'rajesh.kumar@example.com',
    deviceType: CardiacDeviceType.ICD,
    deviceManufacturer: 'Medtronic',
    deviceModel: 'Evera XT',
    deviceSerialNumber: 'MDT-ICD-0001',
    implantDate: '2022-03-15T00:00:00Z',
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z'
  };

  function submitForm(): void {
    fixture.debugElement.query(By.css('form')).triggerEventHandler('ngSubmit', null);
    fixture.detectChanges();
  }

  async function setup(patient: Patient = existingPatient): Promise<void> {
    actionsSubject = new Subject();

    await TestBed.configureTestingModule({
      imports: [EditPatientFormComponent],
      providers: [
        provideTranslateService(),
        provideMockStore({ initialState: {} }),
        provideMockActions(() => actionsSubject.asObservable())
      ]
    }).compileComponents();

    useEnglishTestTranslations(TestBed.inject(TranslateService));

    store = TestBed.inject(MockStore);
    store.overrideSelector(selectPatientUpdating, false);

    fixture = TestBed.createComponent(EditPatientFormComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('patient', patient);
    fixture.detectChanges();
  }

  it('pre-fills the form with the given patient\'s current details', async () => {
    await setup();

    expect(component.form.getRawValue()).toEqual({
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
    });

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect((el.querySelector('#editFirstName') as HTMLInputElement).value).toBe('Rajesh');
    expect((el.querySelector('#editMrn') as HTMLInputElement).value).toBe('APL-1001');
  });

  it('dispatches updatePatient with the edited fields for the correct patient id', async () => {
    await setup();
    jest.spyOn(store, 'dispatch');

    (fixture.debugElement.query(By.css('#editLastName')).nativeElement as HTMLInputElement).value = 'Kumar-Singh';
    fixture.debugElement.query(By.css('#editLastName')).nativeElement.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    submitForm();

    expect(store.dispatch).toHaveBeenCalledWith(
      PatientsActions.updatePatient({
        id: 1,
        patient: expect.objectContaining({
          medicalRecordNumber: 'APL-1001',
          firstName: 'Rajesh',
          lastName: 'Kumar-Singh'
        })
      })
    );
  });

  it('does not dispatch when required fields are cleared', async () => {
    await setup();
    jest.spyOn(store, 'dispatch');

    component.form.patchValue({ firstName: '' });
    submitForm();

    expect(store.dispatch).not.toHaveBeenCalled();
    expect((fixture.debugElement.nativeElement as HTMLElement).textContent).toContain('First name is required.');
  });

  it('shows a success message and emits close when the update succeeds', async () => {
    jest.useFakeTimers();
    await setup();

    const closeSpy = jest.fn();
    component.close.subscribe(closeSpy);

    actionsSubject.next(PatientsActions.updatePatientSuccess({ patient: { ...existingPatient, lastName: 'Kumar-Singh' } }));
    fixture.detectChanges();

    expect(component.submitSucceeded).toBe(true);
    expect((fixture.debugElement.nativeElement as HTMLElement).textContent).toContain('Patient updated successfully.');

    jest.advanceTimersByTime(1200);
    expect(closeSpy).toHaveBeenCalled();
    jest.useRealTimers();
  });

  it('shows the server error message when the update fails', async () => {
    await setup();

    actionsSubject.next(PatientsActions.updatePatientFailure({ error: 'MRN already in use.' }));
    fixture.detectChanges();

    expect((fixture.debugElement.nativeElement as HTMLElement).textContent).toContain('MRN already in use.');
  });
});
