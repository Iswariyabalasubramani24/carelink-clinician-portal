import { Routes } from '@angular/router';

import { adminGuard } from './core/guards/admin.guard';
import { authGuard } from './core/guards/auth.guard';
import { superAdminGuard } from './core/guards/super-admin.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent)
  },
  {
    path: 'patients',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/patients/patients-list.component').then((m) => m.PatientsListComponent)
  },
  {
    path: 'patients/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/patients/patient-detail/patient-detail.component').then(
        (m) => m.PatientDetailComponent
      )
  },
  {
    path: 'transmission-schedule',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/transmission-schedule/transmission-schedule.component').then(
        (m) => m.TransmissionScheduleComponent
      )
  },
  {
    path: 'clinic-management',
    canActivate: [authGuard, adminGuard],
    loadComponent: () =>
      import('./features/clinic-management/clinic-management.component').then(
        (m) => m.ClinicManagementComponent
      )
  },
  {
    path: 'audit-log',
    canActivate: [authGuard, adminGuard],
    loadComponent: () =>
      import('./features/audit-log/audit-log.component').then((m) => m.AuditLogComponent)
  },
  {
    path: 'hospitals',
    canActivate: [authGuard, superAdminGuard],
    loadComponent: () =>
      import('./features/hospitals/hospitals.component').then((m) => m.HospitalsComponent)
  }
];
