import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';

import { DashboardSummary } from '../../core/models/dashboard-summary.model';
import { DashboardService } from '../../core/services/dashboard.service';
import { useEnglishTestTranslations } from '../../core/testing/translate-testing';
import { DashboardComponent } from './dashboard.component';

describe('DashboardComponent', () => {
  let fixture: ComponentFixture<DashboardComponent>;
  let dashboardServiceMock: { getSummary: jest.Mock };
  let routerMock: { navigateByUrl: jest.Mock };

  const summary: DashboardSummary = {
    newPatientsCount: 3,
    disconnectedMonitorsCount: 2,
    totalActivePatientsCount: 17
  };

  async function setup(getSummaryReturn = of(summary)): Promise<void> {
    dashboardServiceMock = { getSummary: jest.fn().mockReturnValue(getSummaryReturn) };
    routerMock = { navigateByUrl: jest.fn() };

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideTranslateService(),
        { provide: DashboardService, useValue: dashboardServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    }).compileComponents();

    useEnglishTestTranslations(TestBed.inject(TranslateService));

    fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
  }

  it('renders the three widget cards with the counts returned by the summary endpoint', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const counts = Array.from(el.querySelectorAll('.widget-card__count')).map((n) => n.textContent?.trim());

    expect(counts).toEqual(['3', '2', '17']);
    expect(el.textContent).toContain('New Patients This Week');
    expect(el.textContent).toContain('Disconnected Monitors');
    expect(el.textContent).toContain('Total Active Patients');
  });

  it('navigates to /patients when a widget card is clicked', async () => {
    await setup();

    const firstWidget: HTMLButtonElement = fixture.debugElement.nativeElement.querySelector('.widget-card');
    firstWidget.click();

    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/patients');
  });

  it('shows an error message instead of widgets when the summary request fails', async () => {
    await setup(throwError(() => new Error('network error')));

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('.widget-card')).toBeNull();
    expect(el.textContent).toContain('Failed to load dashboard summary.');
  });
});
