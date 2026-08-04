import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import {
  ClinicianRole,
  ClinicUser,
  CreateClinicUserResult,
  ResetClinicianPasswordResult
} from '../../core/models/clinic-user.model';
import { ClinicUserService } from '../../core/services/clinic-user.service';

type StatusFilter = 'all' | 'active' | 'suspended';

@Component({
  selector: 'app-clinic-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './clinic-management.component.html',
  styleUrl: './clinic-management.component.scss'
})
export class ClinicManagementComponent implements OnInit {
  users: ClinicUser[] = [];
  usersLoading = true;
  usersError = false;

  statusFilter: StatusFilter = 'all';

  showCreateForm = false;
  creating = false;
  createError = false;
  createdResult: CreateClinicUserResult | null = null;

  resettingUserId: number | null = null;
  resetResult: ResetClinicianPasswordResult | null = null;
  resetError = false;

  readonly ClinicianRole = ClinicianRole;

  readonly createForm = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    languageCode: ['en', Validators.required],
    role: [ClinicianRole.Clinician, Validators.required]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly clinicUserService: ClinicUserService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.usersLoading = true;
    this.usersError = false;
    this.clinicUserService.getAll().subscribe({
      next: (users) => {
        this.users = users;
        this.usersLoading = false;
      },
      error: () => {
        this.usersError = true;
        this.usersLoading = false;
      }
    });
  }

  get filteredUsers(): ClinicUser[] {
    if (this.statusFilter === 'active') {
      return this.users.filter((u) => u.isActive);
    }
    if (this.statusFilter === 'suspended') {
      return this.users.filter((u) => !u.isActive);
    }
    return this.users;
  }

  setStatusFilter(filter: StatusFilter): void {
    this.statusFilter = filter;
  }

  openCreateForm(): void {
    this.showCreateForm = true;
    this.createError = false;
  }

  closeCreateForm(): void {
    this.showCreateForm = false;
    this.createForm.reset({ firstName: '', lastName: '', email: '', languageCode: 'en', role: ClinicianRole.Clinician });
  }

  onSubmitCreate(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.creating = true;
    this.createError = false;

    this.clinicUserService.create(this.createForm.getRawValue()).subscribe({
      next: (result) => {
        this.creating = false;
        this.closeCreateForm();
        this.createdResult = result;
        this.loadUsers();
      },
      error: () => {
        this.creating = false;
        this.createError = true;
      }
    });
  }

  dismissTempPassword(): void {
    this.createdResult = null;
  }

  toggleStatus(user: ClinicUser): void {
    const action$ = user.isActive ? this.clinicUserService.suspend(user.id) : this.clinicUserService.activate(user.id);
    action$.subscribe({
      next: (updated) => {
        this.users = this.users.map((u) => (u.id === updated.id ? updated : u));
      }
    });
  }

  resetPassword(user: ClinicUser): void {
    this.resettingUserId = user.id;
    this.resetError = false;

    this.clinicUserService.resetPassword(user.id).subscribe({
      next: (result) => {
        this.resettingUserId = null;
        this.resetResult = result;
      },
      error: () => {
        this.resettingUserId = null;
        this.resetError = true;
      }
    });
  }

  dismissResetPassword(): void {
    this.resetResult = null;
  }
}
