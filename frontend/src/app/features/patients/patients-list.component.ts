import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { TranslateModule } from '@ngx-translate/core';
import { Observable } from 'rxjs';

import { CardiacDeviceType, Patient, PatientSearchFilters } from '../../core/models/patient.model';
import { PatientsActions } from '../../store/patients/patients.actions';
import {
  selectAllPatients,
  selectPatientsError,
  selectPatientsLoading
} from '../../store/patients/patients.selectors';
import { AddPatientFormComponent } from './add-patient-form/add-patient-form.component';

@Component({
  selector: 'app-patients-list',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, TranslateModule, AddPatientFormComponent],
  templateUrl: './patients-list.component.html',
  styleUrl: './patients-list.component.scss'
})
export class PatientsListComponent implements OnInit {
  patients$: Observable<Patient[]> = this.store.select(selectAllPatients);
  loading$: Observable<boolean> = this.store.select(selectPatientsLoading);
  error$: Observable<string | null> = this.store.select(selectPatientsError);

  showAddForm = false;
  showAdvancedSearch = false;
  hasActiveFilters = false;

  readonly deviceTypes: { value: CardiacDeviceType; labelKey: string }[] = [
    { value: CardiacDeviceType.ICD, labelKey: 'addPatientForm.deviceTypes.icd' },
    { value: CardiacDeviceType.Pacemaker, labelKey: 'addPatientForm.deviceTypes.pacemaker' },
    { value: CardiacDeviceType.CRT_P, labelKey: 'addPatientForm.deviceTypes.crtP' },
    { value: CardiacDeviceType.CRT_D, labelKey: 'addPatientForm.deviceTypes.crtD' },
    { value: CardiacDeviceType.ICM, labelKey: 'addPatientForm.deviceTypes.icm' }
  ];

  readonly searchForm = this.fb.nonNullable.group({
    deviceType: [''],
    status: [''],
    implantDateFrom: [''],
    implantDateTo: [''],
    keyword: ['']
  });

  constructor(
    private readonly store: Store,
    private readonly fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.store.dispatch(PatientsActions.loadPatients());
  }

  openAddForm(): void {
    this.showAddForm = true;
  }

  closeAddForm(): void {
    this.showAddForm = false;
  }

  toggleAdvancedSearch(): void {
    this.showAdvancedSearch = !this.showAdvancedSearch;
  }

  search(): void {
    const raw = this.searchForm.getRawValue();

    const filters: PatientSearchFilters = {
      deviceType: (raw.deviceType as CardiacDeviceType) || undefined,
      isActive: raw.status === '' ? undefined : raw.status === 'active',
      implantDateFrom: raw.implantDateFrom || undefined,
      implantDateTo: raw.implantDateTo || undefined,
      keyword: raw.keyword.trim() || undefined
    };

    this.hasActiveFilters = true;
    this.store.dispatch(PatientsActions.searchPatients({ filters }));
  }

  clearFilters(): void {
    this.searchForm.reset({ deviceType: '', status: '', implantDateFrom: '', implantDateTo: '', keyword: '' });
    this.hasActiveFilters = false;
    this.store.dispatch(PatientsActions.loadPatients());
  }
}
