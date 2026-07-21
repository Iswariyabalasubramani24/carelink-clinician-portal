import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { Hospital, ProvisionHospitalResult } from '../../core/models/hospital.model';
import { HospitalService } from '../../core/services/hospital.service';

@Component({
  selector: 'app-hospitals',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './hospitals.component.html',
  styleUrl: './hospitals.component.scss'
})
export class HospitalsComponent implements OnInit {
  hospitals: Hospital[] = [];
  loading = true;
  error = false;

  showCreateForm = false;
  creating = false;
  createError = false;
  createdResult: ProvisionHospitalResult | null = null;

  readonly createForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    region: ['', Validators.required],
    languageCode: ['en', Validators.required],
    adminFirstName: ['', Validators.required],
    adminLastName: ['', Validators.required],
    adminEmail: ['', [Validators.required, Validators.email]]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly hospitalService: HospitalService
  ) {}

  ngOnInit(): void {
    this.loadHospitals();
  }

  loadHospitals(): void {
    this.loading = true;
    this.error = false;
    this.hospitalService.getAll().subscribe({
      next: (hospitals) => {
        this.hospitals = hospitals;
        this.loading = false;
      },
      error: () => {
        this.error = true;
        this.loading = false;
      }
    });
  }

  openCreateForm(): void {
    this.showCreateForm = true;
    this.createError = false;
  }

  closeCreateForm(): void {
    this.showCreateForm = false;
    this.createForm.reset({
      name: '',
      region: '',
      languageCode: 'en',
      adminFirstName: '',
      adminLastName: '',
      adminEmail: ''
    });
  }

  onSubmitCreate(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.creating = true;
    this.createError = false;

    this.hospitalService.provision(this.createForm.getRawValue()).subscribe({
      next: (result) => {
        this.creating = false;
        this.closeCreateForm();
        this.createdResult = result;
        this.loadHospitals();
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
}
