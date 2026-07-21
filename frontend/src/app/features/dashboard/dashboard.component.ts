import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { Alert } from '../../core/models/alert.model';
import { DashboardSummary } from '../../core/models/dashboard-summary.model';
import { TransmissionScheduleEntry } from '../../core/models/transmission-schedule.model';
import { AlertService } from '../../core/services/alert.service';
import { AuthService } from '../../core/services/auth.service';
import { DashboardService } from '../../core/services/dashboard.service';
import { PatientService } from '../../core/services/patient.service';
import { ScheduleService } from '../../core/services/schedule.service';

const RECENT_ALERTS_LIMIT = 5;
const UPCOMING_TRANSMISSIONS_LIMIT = 5;

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  summary: DashboardSummary | null = null;
  loading = true;
  error = false;

  isAdmin = false;

  recentAlerts: Alert[] = [];
  alertsLoading = true;
  patientNamesById: Record<number, string> = {};

  upcomingTransmissions: TransmissionScheduleEntry[] = [];
  transmissionsLoading = true;

  constructor(
    private readonly dashboardService: DashboardService,
    private readonly alertService: AlertService,
    private readonly scheduleService: ScheduleService,
    private readonly patientService: PatientService,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.getCurrentClinician()?.role === 'Admin';

    this.dashboardService.getSummary().subscribe({
      next: (summary) => {
        this.summary = summary;
        this.loading = false;
      },
      error: () => {
        this.error = true;
        this.loading = false;
      }
    });

    // Recent Alerts needs patient names, but AlertDto only carries patientId -
    // cheapest path is a client-side join against the patient list rather than
    // widening a shared backend contract for one dashboard panel.
    this.patientService.getAll().subscribe({
      next: (patients) => {
        this.patientNamesById = Object.fromEntries(
          patients.map((p) => [p.id, `${p.firstName} ${p.lastName}`])
        );
      }
    });

    this.alertService.getActive().subscribe({
      next: (alerts) => {
        this.recentAlerts = [...alerts]
          .sort((a, b) => new Date(b.triggeredAt).getTime() - new Date(a.triggeredAt).getTime())
          .slice(0, RECENT_ALERTS_LIMIT);
        this.alertsLoading = false;
      },
      error: () => {
        this.alertsLoading = false;
      }
    });

    this.scheduleService.getTransmissionSchedule().subscribe({
      next: (entries) => {
        // Already sorted soonest-first by the backend; only "actually upcoming"
        // (non-null) dates belong in this panel, never-synced patients don't.
        this.upcomingTransmissions = entries
          .filter((e) => e.nextScheduledDate !== null)
          .slice(0, UPCOMING_TRANSMISSIONS_LIMIT);
        this.transmissionsLoading = false;
      },
      error: () => {
        this.transmissionsLoading = false;
      }
    });
  }

  goToPatients(): void {
    this.router.navigateByUrl('/patients');
  }
}
