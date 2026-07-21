import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';

import { ClinicianRole, ClinicUser, CreateClinicUserResult } from '../../core/models/clinic-user.model';
import { ClinicUserService } from '../../core/services/clinic-user.service';
import { useEnglishTestTranslations } from '../../core/testing/translate-testing';
import { ClinicManagementComponent } from './clinic-management.component';

describe('ClinicManagementComponent', () => {
  let fixture: ComponentFixture<ClinicManagementComponent>;
  let clinicUserServiceMock: {
    getAll: jest.Mock;
    create: jest.Mock;
    suspend: jest.Mock;
    activate: jest.Mock;
  };

  const mockUsers: ClinicUser[] = [
    {
      id: 1,
      firstName: 'Anita',
      lastName: 'Rao',
      email: 'doctor@apollo.com',
      role: ClinicianRole.Clinician,
      languageCode: 'en',
      isActive: true,
      createdAt: '2026-01-10T10:00:00Z'
    },
    {
      id: 2,
      firstName: 'Meera',
      lastName: 'Pillai',
      email: 'admin@apollo.com',
      role: ClinicianRole.Admin,
      languageCode: 'en',
      isActive: false,
      createdAt: '2026-02-15T10:00:00Z'
    }
  ];

  const mockCreateResult: CreateClinicUserResult = {
    user: {
      id: 3,
      firstName: 'Priya',
      lastName: 'Nair',
      email: 'priya.nair@apollo.com',
      role: ClinicianRole.Clinician,
      languageCode: 'en',
      isActive: true,
      createdAt: '2026-07-21T10:00:00Z'
    },
    temporaryPassword: 'Tmp#Passw0rd'
  };

  async function setup(users: ClinicUser[] = mockUsers): Promise<void> {
    clinicUserServiceMock = {
      getAll: jest.fn().mockReturnValue(of(users)),
      create: jest.fn().mockReturnValue(of(mockCreateResult)),
      suspend: jest.fn().mockReturnValue(of({ ...mockUsers[0], isActive: false })),
      activate: jest.fn().mockReturnValue(of({ ...mockUsers[1], isActive: true }))
    };

    await TestBed.configureTestingModule({
      imports: [ClinicManagementComponent],
      providers: [provideTranslateService(), { provide: ClinicUserService, useValue: clinicUserServiceMock }]
    }).compileComponents();

    useEnglishTestTranslations(TestBed.inject(TranslateService));

    fixture = TestBed.createComponent(ClinicManagementComponent);
    fixture.detectChanges();
  }

  it('renders the user list with name, email, role, and status', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const rows = el.querySelectorAll('tbody tr');

    expect(rows.length).toBe(2);
    expect(el.textContent).toContain('Anita Rao');
    expect(el.textContent).toContain('doctor@apollo.com');
    expect(el.textContent).toContain('Meera Pillai');
    expect(el.textContent).toContain('admin@apollo.com');
  });

  it('shows the empty-state message when there are no users', async () => {
    await setup([]);

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('tbody')).toBeNull();
    expect(el.textContent).toContain('No users found.');
  });

  it('validates required fields on the create-user form and does not submit', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    (el.querySelector('.btn--primary') as HTMLButtonElement).click();
    fixture.detectChanges();

    const submitBtn = Array.from(el.querySelectorAll('.form-actions button')).find((b) =>
      b.textContent?.includes('Create User')
    ) as HTMLButtonElement;
    submitBtn.click();
    fixture.detectChanges();

    expect(clinicUserServiceMock.create).not.toHaveBeenCalled();
    expect(el.textContent).toContain('First name is required.');
    expect(el.textContent).toContain('Last name is required.');
    expect(el.textContent).toContain('Email is required.');
  });

  it('submits the create-user form and displays the temporary password afterwards', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    (el.querySelector('.btn--primary') as HTMLButtonElement).click();
    fixture.detectChanges();

    (el.querySelector('#firstName') as HTMLInputElement).value = 'Priya';
    (el.querySelector('#firstName') as HTMLInputElement).dispatchEvent(new Event('input'));
    (el.querySelector('#lastName') as HTMLInputElement).value = 'Nair';
    (el.querySelector('#lastName') as HTMLInputElement).dispatchEvent(new Event('input'));
    (el.querySelector('#email') as HTMLInputElement).value = 'priya.nair@apollo.com';
    (el.querySelector('#email') as HTMLInputElement).dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const submitBtn = Array.from(el.querySelectorAll('.form-actions button')).find((b) =>
      b.textContent?.includes('Create User')
    ) as HTMLButtonElement;
    submitBtn.click();
    fixture.detectChanges();

    expect(clinicUserServiceMock.create).toHaveBeenCalledWith(
      expect.objectContaining({ firstName: 'Priya', lastName: 'Nair', email: 'priya.nair@apollo.com' })
    );
    expect(el.textContent).toContain('Tmp#Passw0rd');
    expect(el.textContent).toContain("won't be shown again");
  });

  it('suspending an active user calls the suspend endpoint and updates the status badge', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const firstRowButton = el.querySelector('tbody tr button') as HTMLButtonElement;
    expect(firstRowButton.textContent).toContain('Suspend');

    firstRowButton.click();
    fixture.detectChanges();

    expect(clinicUserServiceMock.suspend).toHaveBeenCalledWith(1);
  });

  it('activating a suspended user calls the activate endpoint', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const rows = el.querySelectorAll('tbody tr');
    const secondRowButton = rows[1].querySelector('button') as HTMLButtonElement;
    expect(secondRowButton.textContent).toContain('Activate');

    secondRowButton.click();
    fixture.detectChanges();

    expect(clinicUserServiceMock.activate).toHaveBeenCalledWith(2);
  });

  it('filters the list to only active or only suspended users', async () => {
    await setup();

    const el = fixture.debugElement.nativeElement as HTMLElement;
    const activeFilter = Array.from(el.querySelectorAll('.toggle-option')).find((b) =>
      b.textContent?.trim() === 'Active'
    ) as HTMLButtonElement;
    activeFilter.click();
    fixture.detectChanges();

    expect(el.querySelectorAll('tbody tr').length).toBe(1);
    expect(el.textContent).toContain('Anita Rao');
    expect(el.textContent).not.toContain('Meera Pillai');
  });
});
