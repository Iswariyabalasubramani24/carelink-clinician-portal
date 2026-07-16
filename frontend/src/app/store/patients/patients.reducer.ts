import { EntityState, createEntityAdapter } from '@ngrx/entity';
import { createFeature, createReducer, on } from '@ngrx/store';

import { Patient } from '../../core/models/patient.model';
import { PatientsActions } from './patients.actions';

export interface PatientsState extends EntityState<Patient> {
  loading: boolean;
  error: string | null;
}

export const patientsAdapter = createEntityAdapter<Patient>();

const initialState: PatientsState = patientsAdapter.getInitialState({
  loading: false,
  error: null
});

export const patientsFeature = createFeature({
  name: 'patients',
  reducer: createReducer(
    initialState,
    on(PatientsActions.loadPatients, (state) => ({ ...state, loading: true, error: null })),
    on(PatientsActions.loadPatientsSuccess, (state, { patients }) =>
      patientsAdapter.setAll(patients, { ...state, loading: false })
    ),
    on(PatientsActions.loadPatientsFailure, (state, { error }) => ({
      ...state,
      loading: false,
      error
    }))
  )
});
