import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { BehaviorSubject, of, throwError } from 'rxjs';

import { Clinician } from '../../models/auth.model';
import { Tenant } from '../../models/tenant.model';
import { AuthService } from '../../services/auth.service';
import { useEnglishTestTranslations } from '../../testing/translate-testing';
import { PatientsActions } from '../../../store/patients/patients.actions';
import { ClinicSwitcherComponent } from './clinic-switcher.component';

describe('ClinicSwitcherComponent', () => {
  let fixture: ComponentFixture<ClinicSwitcherComponent>;
  let component: ClinicSwitcherComponent;
  let store: MockStore;
  let currentClinician$: BehaviorSubject<Clinician | null>;
  let authServiceMock: {
    currentClinician$: BehaviorSubject<Clinician | null>;
    getMyTenants: jest.Mock;
    switchTenant: jest.Mock;
  };

  const apollo: Tenant = { id: 1, name: 'Apollo Hospital', region: 'India', languageCode: 'en' };
  const charite: Tenant = { id: 2, name: 'Charite Hospital', region: 'Germany', languageCode: 'de' };

  const makeClinician = (tenantId: number): Clinician => ({
    id: 1,
    email: 'doctor@apollo.com',
    firstName: 'Anita',
    lastName: 'Rao',
    role: 'Clinician',
    tenantId
  });

  async function setup(tenants: Tenant[], tenantId = 1): Promise<void> {
    currentClinician$ = new BehaviorSubject<Clinician | null>(makeClinician(tenantId));
    authServiceMock = {
      currentClinician$,
      getMyTenants: jest.fn().mockReturnValue(of(tenants)),
      switchTenant: jest.fn().mockReturnValue(of({ accessToken: 'new-token', accessTokenExpiresAt: '', tenantId: 2 }))
    };

    await TestBed.configureTestingModule({
      imports: [ClinicSwitcherComponent],
      providers: [
        provideTranslateService(),
        provideMockStore({ initialState: {} }),
        { provide: AuthService, useValue: authServiceMock }
      ]
    }).compileComponents();

    useEnglishTestTranslations(TestBed.inject(TranslateService));

    store = TestBed.inject(MockStore);
    jest.spyOn(store, 'dispatch');

    fixture = TestBed.createComponent(ClinicSwitcherComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('renders the current tenant name and a dropdown of the other accessible hospitals', async () => {
    await setup([apollo, charite], 1);

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('.clinic-switcher__current')?.textContent).toContain('Apollo Hospital');

    const options = fixture.debugElement.queryAll(By.css('select option[value]:not([value=""])'));
    expect(options).toHaveLength(1);
    expect((options[0].nativeElement as HTMLOptionElement).textContent).toContain('Charite Hospital');
  });

  it('hides the dropdown when the clinician has access to only one hospital', async () => {
    await setup([apollo], 1);

    const el = fixture.debugElement.nativeElement as HTMLElement;
    expect(el.querySelector('.clinic-switcher__current')?.textContent).toContain('Apollo Hospital');
    expect(fixture.debugElement.query(By.css('select'))).toBeNull();
  });

  it('selecting another hospital calls switchTenant and dispatches loadPatients', async () => {
    await setup([apollo, charite], 1);

    const select: HTMLSelectElement = fixture.debugElement.query(By.css('select')).nativeElement;
    select.value = '2';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(authServiceMock.switchTenant).toHaveBeenCalledWith(2);
    expect(store.dispatch).toHaveBeenCalledWith(PatientsActions.loadPatients());
    expect(component.switchControl.value).toBe('');
  });

  it('reverts the selection to the placeholder if the switch fails', async () => {
    await setup([apollo, charite], 1);
    authServiceMock.switchTenant.mockReturnValue(throwError(() => new Error('403')));

    const select: HTMLSelectElement = fixture.debugElement.query(By.css('select')).nativeElement;
    select.value = '2';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(authServiceMock.switchTenant).toHaveBeenCalledWith(2);
    expect(component.switchControl.value).toBe('');
    expect(store.dispatch).not.toHaveBeenCalled();
  });
});
