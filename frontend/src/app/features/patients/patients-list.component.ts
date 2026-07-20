import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { TranslateModule } from '@ngx-translate/core';
import { Observable } from 'rxjs';

import { Patient } from '../../core/models/patient.model';
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
  imports: [CommonModule, RouterLink, TranslateModule, AddPatientFormComponent],
  templateUrl: './patients-list.component.html',
  styleUrl: './patients-list.component.scss'
})
export class PatientsListComponent implements OnInit {
  patients$: Observable<Patient[]> = this.store.select(selectAllPatients);
  loading$: Observable<boolean> = this.store.select(selectPatientsLoading);
  error$: Observable<string | null> = this.store.select(selectPatientsError);

  showAddForm = false;

  constructor(private readonly store: Store) {}

  ngOnInit(): void {
    this.store.dispatch(PatientsActions.loadPatients());
  }

  openAddForm(): void {
    this.showAddForm = true;
  }

  closeAddForm(): void {
    this.showAddForm = false;
  }
}
