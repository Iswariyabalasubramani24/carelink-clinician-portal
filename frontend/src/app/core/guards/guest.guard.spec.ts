import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';

import { AuthService } from '../services/auth.service';
import { guestGuard } from './guest.guard';

describe('guestGuard', () => {
  let authServiceMock: { isAuthenticated: jest.Mock; getCurrentClinician: jest.Mock };
  let routerMock: { createUrlTree: jest.Mock };

  beforeEach(() => {
    authServiceMock = { isAuthenticated: jest.fn(), getCurrentClinician: jest.fn() };
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
      guestGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot)
    );
  }

  it('allows a signed-out visitor to reach the login page', () => {
    authServiceMock.isAuthenticated.mockReturnValue(false);

    expect(runGuard()).toBe(true);
    expect(routerMock.createUrlTree).not.toHaveBeenCalled();
  });

  it('redirects an authenticated clinician to the dashboard', () => {
    authServiceMock.isAuthenticated.mockReturnValue(true);
    authServiceMock.getCurrentClinician.mockReturnValue({ role: 'Clinician' });
    const urlTree = {} as UrlTree;
    routerMock.createUrlTree.mockReturnValue(urlTree);

    const result = runGuard();

    expect(routerMock.createUrlTree).toHaveBeenCalledWith(['/dashboard']);
    expect(result).toBe(urlTree);
  });

  it('redirects an authenticated SuperAdmin to the hospitals page', () => {
    authServiceMock.isAuthenticated.mockReturnValue(true);
    authServiceMock.getCurrentClinician.mockReturnValue({ role: 'SuperAdmin' });
    const urlTree = {} as UrlTree;
    routerMock.createUrlTree.mockReturnValue(urlTree);

    const result = runGuard();

    expect(routerMock.createUrlTree).toHaveBeenCalledWith(['/hospitals']);
    expect(result).toBe(urlTree);
  });
});
