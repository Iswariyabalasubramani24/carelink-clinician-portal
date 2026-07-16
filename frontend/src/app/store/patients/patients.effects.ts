import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, of, switchMap } from 'rxjs';

import { PatientService } from '../../core/services/patient.service';
import { PatientsActions } from './patients.actions';

@Injectable()
export class PatientsEffects {
  loadPatients$ = createEffect(() =>
    this.actions$.pipe(
      ofType(PatientsActions.loadPatients),
      switchMap(() =>
        this.patientService.getAll().pipe(
          map((patients) => PatientsActions.loadPatientsSuccess({ patients })),
          catchError((error) =>
            of(PatientsActions.loadPatientsFailure({ error: error.message ?? 'Unknown error' }))
          )
        )
      )
    )
  );

  constructor(
    private readonly actions$: Actions,
    private readonly patientService: PatientService
  ) {}
}
