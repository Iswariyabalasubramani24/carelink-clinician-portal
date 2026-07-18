import { HttpEvent, HttpHandlerFn, HttpRequest } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { AuthService } from '../services/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let authServiceMock: { getAccessToken: jest.Mock };

  beforeEach(() => {
    authServiceMock = { getAccessToken: jest.fn() };

    TestBed.configureTestingModule({
      providers: [{ provide: AuthService, useValue: authServiceMock }]
    });
  });

  function run(req: HttpRequest<unknown>, next: HttpHandlerFn) {
    return TestBed.runInInjectionContext(() => authInterceptor(req, next));
  }

  it('attaches the Authorization header when an access token is present', () => {
    authServiceMock.getAccessToken.mockReturnValue('my-access-token');
    const req = new HttpRequest('GET', '/api/patients');
    let capturedRequest!: HttpRequest<unknown>;

    const next: HttpHandlerFn = (r) => {
      capturedRequest = r;
      return of({} as HttpEvent<unknown>);
    };

    run(req, next);

    expect(capturedRequest.headers.get('Authorization')).toBe('Bearer my-access-token');
  });

  it('does not attach an Authorization header when there is no access token', () => {
    authServiceMock.getAccessToken.mockReturnValue(null);
    const req = new HttpRequest('GET', '/api/patients');
    let capturedRequest!: HttpRequest<unknown>;

    const next: HttpHandlerFn = (r) => {
      capturedRequest = r;
      return of({} as HttpEvent<unknown>);
    };

    run(req, next);

    expect(capturedRequest.headers.has('Authorization')).toBe(false);
  });
});
