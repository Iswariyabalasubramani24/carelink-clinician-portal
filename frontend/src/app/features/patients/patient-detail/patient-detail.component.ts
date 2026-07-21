import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { TranslateModule } from '@ngx-translate/core';
import { ChartConfiguration, ChartData } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { Observable } from 'rxjs';

import { Alert, AlertType, AlertUrgency, PatientAlertSetting } from '../../../core/models/alert.model';
import { Patient } from '../../../core/models/patient.model';
import { TransmissionHistoryPoint } from '../../../core/models/transmission-history.model';
import { AlertService } from '../../../core/services/alert.service';
import { PatientService } from '../../../core/services/patient.service';
import { PatientsActions } from '../../../store/patients/patients.actions';
import { selectAllPatients, selectPatientsLoading } from '../../../store/patients/patients.selectors';

type DetailTab = 'overview' | 'equipment' | 'history' | 'careAlert';

interface AlertSettingRow {
  alertType: AlertType;
  urgencyControl: FormControl<AlertUrgency>;
}

@Component({
  selector: 'app-patient-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, TranslateModule, BaseChartDirective],
  templateUrl: './patient-detail.component.html',
  styleUrl: './patient-detail.component.scss'
})
export class PatientDetailComponent implements OnInit {
  activeTab: DetailTab = 'overview';
  patientId = 0;

  patients$: Observable<Patient[]> = this.store.select(selectAllPatients);
  patientsLoading$: Observable<boolean> = this.store.select(selectPatientsLoading);

  history: TransmissionHistoryPoint[] = [];
  historyLoading = true;
  historyError = false;

  heartRateChartData: ChartData<'line'> = { labels: [], datasets: [] };
  batteryChartData: ChartData<'line'> = { labels: [], datasets: [] };
  readonly chartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false
  };

  alerts: Alert[] = [];
  alertsLoading = true;

  alertSettingRows: AlertSettingRow[] = [];
  alertSettingsLoading = true;
  useOverride = false;
  saveSucceeded = false;
  saveError = false;

  readonly AlertUrgency = AlertUrgency;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly store: Store,
    private readonly patientService: PatientService,
    private readonly alertService: AlertService
  ) {}

  ngOnInit(): void {
    this.patientId = Number(this.route.snapshot.paramMap.get('id'));
    this.store.dispatch(PatientsActions.loadPatients());

    this.patientService.getTransmissionHistory(this.patientId).subscribe({
      next: (history) => {
        this.history = history;
        this.buildCharts(history);
        this.historyLoading = false;
      },
      error: () => {
        this.historyError = true;
        this.historyLoading = false;
      }
    });

    this.patientService.getAlerts(this.patientId).subscribe({
      next: (alerts) => {
        this.alerts = alerts;
        this.alertsLoading = false;
      },
      error: () => {
        this.alertsLoading = false;
      }
    });

    this.patientService.getAlertSettings(this.patientId).subscribe({
      next: (settings) => {
        this.applyAlertSettings(settings);
        this.alertSettingsLoading = false;
      },
      error: () => {
        this.alertSettingsLoading = false;
      }
    });
  }

  findPatient(patients: Patient[] | null): Patient | null {
    return patients?.find((p) => p.id === this.patientId) ?? null;
  }

  setTab(tab: DetailTab): void {
    this.activeTab = tab;
  }

  acknowledgeAlert(alert: Alert): void {
    this.alertService.acknowledge(alert.id).subscribe({
      next: () => {
        this.alerts = this.alerts.filter((a) => a.id !== alert.id);
      }
    });
  }

  snoozeAlert(alert: Alert): void {
    this.alertService.snooze(alert.id).subscribe({
      next: () => {
        this.alerts = this.alerts.filter((a) => a.id !== alert.id);
      }
    });
  }

  setUseOverride(useOverride: boolean): void {
    this.useOverride = useOverride;
  }

  saveAlertSettings(): void {
    this.saveSucceeded = false;
    this.saveError = false;

    const overrides = this.useOverride
      ? this.alertSettingRows.map((row) => ({ alertType: row.alertType, urgency: row.urgencyControl.value }))
      : [];

    this.patientService.updateAlertSettings(this.patientId, { useOverride: this.useOverride, overrides }).subscribe({
      next: (settings) => {
        this.applyAlertSettings(settings);
        this.saveSucceeded = true;
      },
      error: () => {
        this.saveError = true;
      }
    });
  }

  private applyAlertSettings(settings: PatientAlertSetting[]): void {
    this.useOverride = settings.some((s) => s.isOverride);
    this.alertSettingRows = settings.map((s) => ({
      alertType: s.alertType,
      urgencyControl: new FormControl<AlertUrgency>(s.effectiveUrgency, { nonNullable: true })
    }));
  }

  private buildCharts(history: TransmissionHistoryPoint[]): void {
    const labels = history.map((point) =>
      new Date(point.date).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
    );

    this.heartRateChartData = {
      labels,
      datasets: [
        {
          data: history.map((point) => point.heartRate),
          label: 'Heart Rate (bpm)',
          borderColor: '#2c6e85',
          backgroundColor: 'rgba(44, 110, 133, 0.1)',
          tension: 0.3
        }
      ]
    };

    this.batteryChartData = {
      labels,
      datasets: [
        {
          data: history.map((point) => point.batteryLevel),
          label: 'Battery Level (%)',
          borderColor: '#2f9e68',
          backgroundColor: 'rgba(47, 158, 104, 0.1)',
          tension: 0.3
        }
      ]
    };
  }
}
