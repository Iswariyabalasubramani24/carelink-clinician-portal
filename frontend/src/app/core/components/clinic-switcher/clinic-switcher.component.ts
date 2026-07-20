import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Store } from '@ngrx/store';
import { TranslateModule } from '@ngx-translate/core';
import { Subject, takeUntil } from 'rxjs';

import { Tenant } from '../../models/tenant.model';
import { AuthService } from '../../services/auth.service';
import { PatientsActions } from '../../../store/patients/patients.actions';

@Component({
  selector: 'app-clinic-switcher',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './clinic-switcher.component.html',
  styleUrl: './clinic-switcher.component.scss'
})
export class ClinicSwitcherComponent implements OnInit, OnDestroy {
  tenants: Tenant[] = [];
  currentTenantId: number | null = null;

  // Always reset back to the empty placeholder after firing a switch - this is
  // an action control, not a persistent selection of the current tenant.
  readonly switchControl = new FormControl<number | ''>('');

  private readonly destroyed$ = new Subject<void>();

  constructor(
    private readonly authService: AuthService,
    private readonly store: Store
  ) {}

  get currentTenantName(): string | null {
    return this.tenants.find((tenant) => tenant.id === this.currentTenantId)?.name ?? null;
  }

  get otherTenants(): Tenant[] {
    return this.tenants.filter((tenant) => tenant.id !== this.currentTenantId);
  }

  ngOnInit(): void {
    this.authService.currentClinician$.pipe(takeUntil(this.destroyed$)).subscribe((clinician) => {
      this.currentTenantId = clinician?.tenantId ?? null;
    });

    this.authService.getMyTenants().subscribe({
      next: (tenants) => (this.tenants = tenants),
      error: () => (this.tenants = [])
    });

    this.switchControl.valueChanges.pipe(takeUntil(this.destroyed$)).subscribe((value) => {
      if (value === '' || value === null) {
        return;
      }
      this.switchTo(Number(value));
    });
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }

  private switchTo(tenantId: number): void {
    this.authService.switchTenant(tenantId).subscribe({
      next: () => {
        this.switchControl.setValue('', { emitEvent: false });
        this.store.dispatch(PatientsActions.loadPatients());
      },
      error: () => {
        this.switchControl.setValue('', { emitEvent: false });
      }
    });
  }
}
