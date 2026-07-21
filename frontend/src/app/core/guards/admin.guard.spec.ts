import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';

import { AuthService } from '../services/auth.service';
import { adminGuard } from './admin.guard';

describe('adminGuard', () => {
  let authServiceMock: { getCurrentClinician: jest.Mock };
  let routerMock: { createUrlTree: jest.Mock };

  beforeEach(() => {
    authServiceMock = { getCurrentClinician: jest.fn() };
    routerMock = { createUrlTree: jest.fn() };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    });
  });

  function runGuard() {
    return TestBed.runInInjectionContext(() =>
      adminGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot)
    );
  }

  it('allows navigation when the current clinician has the Admin role', () => {
    authServiceMock.getCurrentClinician.mockReturnValue({ role: 'Admin' });

    expect(runGuard()).toBe(true);
    expect(routerMock.createUrlTree).not.toHaveBeenCalled();
  });

  it('redirects to /dashboard when the current clinician is not an Admin', () => {
    authServiceMock.getCurrentClinician.mockReturnValue({ role: 'Clinician' });
    const urlTree = {} as UrlTree;
    routerMock.createUrlTree.mockReturnValue(urlTree);

    const result = runGuard();

    expect(routerMock.createUrlTree).toHaveBeenCalledWith(['/dashboard']);
    expect(result).toBe(urlTree);
  });

  it('redirects to /dashboard when there is no current clinician', () => {
    authServiceMock.getCurrentClinician.mockReturnValue(null);
    const urlTree = {} as UrlTree;
    routerMock.createUrlTree.mockReturnValue(urlTree);

    expect(runGuard()).toBe(urlTree);
  });
});
