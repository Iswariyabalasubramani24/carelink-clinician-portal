import { patientsAdapter, patientsFeature } from './patients.reducer';

const { selectAll } = patientsAdapter.getSelectors(patientsFeature.selectPatientsState);

export const selectAllPatients = selectAll;
export const { selectLoading: selectPatientsLoading, selectError: selectPatientsError } =
  patientsFeature;
