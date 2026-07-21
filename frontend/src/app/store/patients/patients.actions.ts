import { createActionGroup, emptyProps, props } from '@ngrx/store';

import { Patient, PatientSearchFilters } from '../../core/models/patient.model';

export const PatientsActions = createActionGroup({
  source: 'Patients',
  events: {
    'Load Patients': emptyProps(),
    'Search Patients': props<{ filters: PatientSearchFilters }>(),
    'Load Patients Success': props<{ patients: Patient[] }>(),
    'Load Patients Failure': props<{ error: string }>(),

    'Create Patient': props<{ patient: Partial<Patient> }>(),
    'Create Patient Success': props<{ patient: Patient }>(),
    'Create Patient Failure': props<{ error: string }>()
  }
});
