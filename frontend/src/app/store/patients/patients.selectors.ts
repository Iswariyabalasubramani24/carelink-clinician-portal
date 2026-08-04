import { patientsAdapter, patientsFeature } from './patients.reducer';

const { selectAll } = patientsAdapter.getSelectors(patientsFeature.selectPatientsState);

export const selectAllPatients = selectAll;
export const { selectLoading: selectPatientsLoading, selectError: selectPatientsError } =
  patientsFeature;
export const { selectCreating: selectPatientCreating, selectCreateError: selectPatientCreateError } =
  patientsFeature;
export const { selectUpdating: selectPatientUpdating, selectUpdateError: selectPatientUpdateError } =
  patientsFeature;
