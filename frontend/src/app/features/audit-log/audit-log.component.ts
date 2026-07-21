import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { AuditLogEntry } from '../../core/models/audit-log.model';
import { AuditLogService } from '../../core/services/audit-log.service';

const PAGE_SIZE = 25;

const KNOWN_ACTIONS = [
  'PatientCreated',
  'AlertAcknowledged',
  'AlertSnoozed',
  'ClinicUserCreated',
  'ClinicUserSuspended',
  'ClinicUserActivated',
  'ReportGenerated'
];

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './audit-log.component.html',
  styleUrl: './audit-log.component.scss'
})
export class AuditLogComponent implements OnInit {
  entries: AuditLogEntry[] = [];
  totalCount = 0;
  page = 1;
  readonly pageSize = PAGE_SIZE;

  loading = true;
  error = false;

  actionFilter = '';
  readonly knownActions = KNOWN_ACTIONS;

  constructor(private readonly auditLogService: AuditLogService) {}

  ngOnInit(): void {
    this.load();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  setActionFilter(action: string): void {
    this.actionFilter = action;
    this.page = 1;
    this.load();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) {
      return;
    }
    this.page = page;
    this.load();
  }

  private load(): void {
    this.loading = true;
    this.error = false;

    this.auditLogService.getAll(this.page, this.pageSize, this.actionFilter || undefined).subscribe({
      next: (result) => {
        this.entries = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
      },
      error: () => {
        this.error = true;
        this.loading = false;
      }
    });
  }
}
