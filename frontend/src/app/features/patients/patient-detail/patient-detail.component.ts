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
import { PatientNote } from '../../../core/models/patient-note.model';
import { Patient } from '../../../core/models/patient.model';
import { PatientReportSettings, Report, ReportType } from '../../../core/models/report.model';
import { PatientScheduleSettings } from '../../../core/models/schedule.model';
import { TransmissionHistoryPoint } from '../../../core/models/transmission-history.model';
import { AlertService } from '../../../core/services/alert.service';
import { PatientService } from '../../../core/services/patient.service';
import { ReportService } from '../../../core/services/report.service';
import { PatientsActions } from '../../../store/patients/patients.actions';
import { selectAllPatients, selectPatientsLoading } from '../../../store/patients/patients.selectors';
import { EditPatientFormComponent } from '../edit-patient-form/edit-patient-form.component';

type DetailTab = 'overview' | 'profile' | 'equipment' | 'schedule' | 'history' | 'careAlert' | 'reports' | 'notes';

interface AlertSettingRow {
  alertType: AlertType;
  urgencyControl: FormControl<AlertUrgency>;
}

@Component({
  selector: 'app-patient-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, TranslateModule, BaseChartDirective, EditPatientFormComponent],
  templateUrl: './patient-detail.component.html',
  styleUrl: './patient-detail.component.scss'
})
export class PatientDetailComponent implements OnInit {
  activeTab: DetailTab = 'overview';
  patientId = 0;
  showEditForm = false;

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

  reports: Report[] = [];
  reportsLoading = true;
  reportTypeControl = new FormControl<ReportType>(ReportType.FullReport, { nonNullable: true });
  generatingReport = false;
  generateReportError = false;

  reportSettingsLoading = true;
  reportUseOverride = false;
  reportIntervalControl = new FormControl<number>(30, { nonNullable: true });
  reportSettingsSaveSucceeded = false;
  reportSettingsSaveError = false;

  readonly ReportType = ReportType;

  scheduleSettingsLoading = true;
  scheduleUseOverride = false;
  scheduleIntervalControl = new FormControl<number>(30, { nonNullable: true });
  scheduleSettingsSaveSucceeded = false;
  scheduleSettingsSaveError = false;

  notes: PatientNote[] = [];
  notesLoading = true;
  newNoteControl = new FormControl<string>('', { nonNullable: true });
  postingNote = false;
  postNoteError = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly store: Store,
    private readonly patientService: PatientService,
    private readonly alertService: AlertService,
    private readonly reportService: ReportService
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

    this.loadReports();

    this.patientService.getReportSettings(this.patientId).subscribe({
      next: (settings) => {
        this.applyReportSettings(settings);
        this.reportSettingsLoading = false;
      },
      error: () => {
        this.reportSettingsLoading = false;
      }
    });

    this.patientService.getScheduleSettings(this.patientId).subscribe({
      next: (settings) => {
        this.applyScheduleSettings(settings);
        this.scheduleSettingsLoading = false;
      },
      error: () => {
        this.scheduleSettingsLoading = false;
      }
    });

    this.loadNotes();
  }

  findPatient(patients: Patient[] | null): Patient | null {
    return patients?.find((p) => p.id === this.patientId) ?? null;
  }

  setTab(tab: DetailTab): void {
    this.activeTab = tab;
  }

  openEditForm(): void {
    this.showEditForm = true;
  }

  closeEditForm(): void {
    this.showEditForm = false;
  }

  toggleActive(patient: Patient): void {
    this.store.dispatch(
      patient.isActive
        ? PatientsActions.deactivatePatient({ id: patient.id })
        : PatientsActions.activatePatient({ id: patient.id })
    );
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

  loadReports(): void {
    this.reportsLoading = true;
    this.patientService.getReports(this.patientId).subscribe({
      next: (reports) => {
        this.reports = reports;
        this.reportsLoading = false;
      },
      error: () => {
        this.reportsLoading = false;
      }
    });
  }

  generateReport(): void {
    this.generatingReport = true;
    this.generateReportError = false;

    this.patientService.generateReport(this.patientId, this.reportTypeControl.value).subscribe({
      next: () => {
        this.generatingReport = false;
        this.loadReports();
      },
      error: () => {
        this.generatingReport = false;
        this.generateReportError = true;
      }
    });
  }

  downloadReport(report: Report): void {
    this.reportService.download(report.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `${report.reportType}-${report.id}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
      }
    });
  }

  setReportUseOverride(useOverride: boolean): void {
    this.reportUseOverride = useOverride;
  }

  saveReportSettings(): void {
    this.reportSettingsSaveSucceeded = false;
    this.reportSettingsSaveError = false;

    this.patientService
      .updateReportSettings(this.patientId, {
        useOverride: this.reportUseOverride,
        intervalDays: this.reportIntervalControl.value
      })
      .subscribe({
        next: (settings) => {
          this.applyReportSettings(settings);
          this.reportSettingsSaveSucceeded = true;
        },
        error: () => {
          this.reportSettingsSaveError = true;
        }
      });
  }

  setScheduleUseOverride(useOverride: boolean): void {
    this.scheduleUseOverride = useOverride;
  }

  saveScheduleSettings(): void {
    this.scheduleSettingsSaveSucceeded = false;
    this.scheduleSettingsSaveError = false;

    this.patientService
      .updateScheduleSettings(this.patientId, {
        useOverride: this.scheduleUseOverride,
        intervalDays: this.scheduleIntervalControl.value
      })
      .subscribe({
        next: (settings) => {
          this.applyScheduleSettings(settings);
          this.scheduleSettingsSaveSucceeded = true;
        },
        error: () => {
          this.scheduleSettingsSaveError = true;
        }
      });
  }

  loadNotes(): void {
    this.notesLoading = true;
    this.patientService.getNotes(this.patientId).subscribe({
      next: (notes) => {
        this.notes = notes;
        this.notesLoading = false;
      },
      error: () => {
        this.notesLoading = false;
      }
    });
  }

  postNote(): void {
    const content = this.newNoteControl.value.trim();
    if (!content) {
      return;
    }

    this.postingNote = true;
    this.postNoteError = false;

    this.patientService.createNote(this.patientId, content).subscribe({
      next: (note) => {
        this.notes = [note, ...this.notes];
        this.newNoteControl.setValue('');
        this.postingNote = false;
      },
      error: () => {
        this.postingNote = false;
        this.postNoteError = true;
      }
    });
  }

  private applyReportSettings(settings: PatientReportSettings): void {
    this.reportUseOverride = settings.isOverride;
    this.reportIntervalControl.setValue(settings.intervalDays);
  }

  private applyScheduleSettings(settings: PatientScheduleSettings): void {
    this.scheduleUseOverride = settings.isOverride;
    this.scheduleIntervalControl.setValue(settings.intervalDays);
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
