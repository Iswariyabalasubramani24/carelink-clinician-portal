import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { TransmissionScheduleEntry } from '../../core/models/transmission-schedule.model';
import { ScheduleService } from '../../core/services/schedule.service';

type SortDirection = 'asc' | 'desc';

@Component({
  selector: 'app-transmission-schedule',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './transmission-schedule.component.html',
  styleUrl: './transmission-schedule.component.scss'
})
export class TransmissionScheduleComponent implements OnInit {
  entries: TransmissionScheduleEntry[] = [];
  loading = true;
  error = false;

  // The backend already returns entries sorted ascending by next scheduled
  // date (soonest due first), so this starting direction matches what's on
  // screen without an extra client-side sort on load.
  sortDirection: SortDirection = 'asc';

  constructor(private readonly scheduleService: ScheduleService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = false;

    this.scheduleService.getTransmissionSchedule().subscribe({
      next: (entries) => {
        this.entries = entries;
        this.loading = false;
      },
      error: () => {
        this.error = true;
        this.loading = false;
      }
    });
  }

  toggleSortByNextScheduledDate(): void {
    this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    const direction = this.sortDirection === 'asc' ? 1 : -1;

    this.entries = [...this.entries].sort((a, b) => {
      // Patients with no computable next date (never synced) always sort last,
      // in either direction, since there's nothing to compare them against.
      if (a.nextScheduledDate === null && b.nextScheduledDate === null) return 0;
      if (a.nextScheduledDate === null) return 1;
      if (b.nextScheduledDate === null) return -1;

      return direction * (new Date(a.nextScheduledDate).getTime() - new Date(b.nextScheduledDate).getTime());
    });
  }
}
