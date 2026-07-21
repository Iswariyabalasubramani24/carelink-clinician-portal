import { EntityState, createEntityAdapter } from '@ngrx/entity';
import { createFeature, createReducer, on } from '@ngrx/store';

import { Patient } from '../../core/models/patient.model';
import { PatientsActions } from './patients.actions';

export interface PatientsState extends EntityState<Patient> {
  loading: boolean;
  error: string | null;
  creating: boolean;
  createError: string | null;
}

export const patientsAdapter = createEntityAdapter<Patient>();

const initialState: PatientsState = patientsAdapter.getInitialState({
  loading: false,
  error: null,
  creating: false,
  createError: null
});

export const patientsFeature = createFeature({
  name: 'patients',
  reducer: createReducer(
    initialState,
    on(PatientsActions.loadPatients, (state) => ({ ...state, loading: true, error: null })),
    on(PatientsActions.searchPatients, (state) => ({ ...state, loading: true, error: null })),
    on(PatientsActions.loadPatientsSuccess, (state, { patients }) =>
      patientsAdapter.setAll(patients, { ...state, loading: false })
    ),
    on(PatientsActions.loadPatientsFailure, (state, { error }) => ({
      ...state,
      loading: false,
      error
    })),

    on(PatientsActions.createPatient, (state) => ({
      ...state,
      creating: true,
      createError: null
    })),
    on(PatientsActions.createPatientSuccess, (state, { patient }) =>
      patientsAdapter.addOne(patient, { ...state, creating: false })
    ),
    on(PatientsActions.createPatientFailure, (state, { error }) => ({
      ...state,
      creating: false,
      createError: error
    }))
  )
});
