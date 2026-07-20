import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { Actions, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { TranslateModule } from '@ngx-translate/core';
import { Observable } from 'rxjs';

import { CardiacDeviceType } from '../../../core/models/patient.model';
import { PatientsActions } from '../../../store/patients/patients.actions';
import { selectPatientCreating } from '../../../store/patients/patients.selectors';

function notInFuture(control: AbstractControl): ValidationErrors | null {
  if (!control.value) {
    return null;
  }
  return new Date(control.value).getTime() > Date.now() ? { futureDate: true } : null;
}

function implantNotBeforeBirth(group: AbstractControl): ValidationErrors | null {
  const dob = group.get('dateOfBirth')?.value;
  const implantDate = group.get('implantDate')?.value;
  if (!dob || !implantDate) {
    return null;
  }
  return new Date(implantDate) < new Date(dob) ? { implantBeforeBirth: true } : null;
}

@Component({
  selector: 'app-add-patient-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './add-patient-form.component.html',
  styleUrl: './add-patient-form.component.scss'
})
export class AddPatientFormComponent {
  @Output() close = new EventEmitter<void>();

  readonly deviceTypes: { value: CardiacDeviceType; labelKey: string }[] = [
    { value: CardiacDeviceType.ICD, labelKey: 'addPatientForm.deviceTypes.icd' },
    { value: CardiacDeviceType.Pacemaker, labelKey: 'addPatientForm.deviceTypes.pacemaker' },
    { value: CardiacDeviceType.CRT_P, labelKey: 'addPatientForm.deviceTypes.crtP' },
    { value: CardiacDeviceType.CRT_D, labelKey: 'addPatientForm.deviceTypes.crtD' },
    { value: CardiacDeviceType.ICM, labelKey: 'addPatientForm.deviceTypes.icm' }
  ];

  readonly form = this.fb.nonNullable.group(
    {
      medicalRecordNumber: ['', Validators.required],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      dateOfBirth: ['', [Validators.required, notInFuture]],
      phoneNumber: [''],
      email: ['', Validators.email],
      deviceType: ['', Validators.required],
      deviceManufacturer: [''],
      deviceModel: [''],
      deviceSerialNumber: ['', Validators.required],
      implantDate: ['', [Validators.required, notInFuture]]
    },
    { validators: implantNotBeforeBirth }
  );

  readonly creating$: Observable<boolean> = this.store.select(selectPatientCreating);
  submitError: string | null = null;
  submitSucceeded = false;

  constructor(
    private readonly fb: FormBuilder,
    private readonly store: Store,
    private readonly actions$: Actions
  ) {
    this.actions$.pipe(ofType(PatientsActions.createPatientSuccess), takeUntilDestroyed()).subscribe(() => {
      this.submitSucceeded = true;
      this.submitError = null;
      setTimeout(() => this.close.emit(), 1100);
    });

    this.actions$
      .pipe(ofType(PatientsActions.createPatientFailure), takeUntilDestroyed())
      .subscribe(({ error }) => {
        this.submitError = error;
      });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitError = null;
    const value = this.form.getRawValue();

    this.store.dispatch(
      PatientsActions.createPatient({
        patient: {
          medicalRecordNumber: value.medicalRecordNumber,
          firstName: value.firstName,
          lastName: value.lastName,
          dateOfBirth: value.dateOfBirth,
          phoneNumber: value.phoneNumber || undefined,
          email: value.email || undefined,
          deviceType: value.deviceType as CardiacDeviceType,
          deviceManufacturer: value.deviceManufacturer || undefined,
          deviceModel: value.deviceModel || undefined,
          deviceSerialNumber: value.deviceSerialNumber,
          implantDate: value.implantDate
        }
      })
    );
  }

  onCancel(): void {
    this.close.emit();
  }
}
