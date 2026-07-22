import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { BehaviorSubject, Subject, of } from 'rxjs';

import { AuthService } from './auth.service';
import { IDLE_TIMEOUT_MS, IdleTimeoutService } from './idle-timeout.service';

describe('IdleTimeoutService', () => {
  let service: IdleTimeoutService;
  let isAuthenticated$: BehaviorSubject<boolean>;
  let authServiceMock: { isAuthenticated$: unknown; logout: jest.Mock };
  let navigateSpy: jest.SpyInstance;

  beforeEach(() => {
    jest.useFakeTimers();

    isAuthenticated$ = new BehaviorSubject<boolean>(false);
    authServiceMock = {
      isAuthenticated$: isAuthenticated$.asObservable(),
      logout: jest.fn().mockReturnValue(of(void 0))
    };

    TestBed.configureTestingModule({
      providers: [{ provide: AuthService, useValue: authServiceMock }, provideRouter([])]
    });

    service = TestBed.inject(IdleTimeoutService);
    navigateSpy = jest.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    service.start();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('does nothing while unauthenticated', () => {
    jest.advanceTimersByTime(IDLE_TIMEOUT_MS + 1000);

    expect(authServiceMock.logout).not.toHaveBeenCalled();
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('logs out and redirects with reason=idle after the idle window elapses', () => {
    isAuthenticated$.next(true);

    jest.advanceTimersByTime(IDLE_TIMEOUT_MS + 1000);

    expect(authServiceMock.logout).toHaveBeenCalledTimes(1);
    expect(navigateSpy).toHaveBeenCalledWith(['/login'], { queryParams: { reason: 'idle' } });
  });

  it('activity before the deadline defers the logout', () => {
    isAuthenticated$.next(true);

    // Just before the deadline, the clinician moves the mouse...
    jest.advanceTimersByTime(IDLE_TIMEOUT_MS - 1000);
    document.dispatchEvent(new Event('mousemove'));

    // ...so crossing the original deadline must NOT log out...
    jest.advanceTimersByTime(2000);
    expect(authServiceMock.logout).not.toHaveBeenCalled();

    // ...but a full idle window after that activity does.
    jest.advanceTimersByTime(IDLE_TIMEOUT_MS);
    expect(authServiceMock.logout).toHaveBeenCalledTimes(1);
  });

  it('stops watching after logout so the timer cannot fire twice', () => {
    isAuthenticated$.next(true);
    jest.advanceTimersByTime(IDLE_TIMEOUT_MS + 1000);
    expect(authServiceMock.logout).toHaveBeenCalledTimes(1);

    // Session ended; further time passing must not trigger another logout.
    isAuthenticated$.next(false);
    jest.advanceTimersByTime(IDLE_TIMEOUT_MS * 2);
    expect(authServiceMock.logout).toHaveBeenCalledTimes(1);
  });
});
