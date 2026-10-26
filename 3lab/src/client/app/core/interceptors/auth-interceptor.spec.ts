import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { authInterceptor, resetRefreshInFlight } from './auth-interceptor';

describe('authInterceptor', () => {
  let http: HttpTestingController;
  let client: HttpClient;

  beforeEach(() => {
    resetRefreshInFlight();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
  });

  afterEach(() => {
    http.verify();
    resetRefreshInFlight();
  });

  it('пропускает запрос без 401 без попыток обновления', () => {
    client.get('/api/v1/catalog/categories').subscribe();

    const req = http.expectOne('/api/v1/catalog/categories');
    expect(req.request.method).toBe('GET');
    req.flush([]);
    http.expectNone('/api/v1/auth/refresh');
  });

  it('при 401 вызывает /auth/refresh и повторяет исходный запрос', () => {
    client.get('/api/v1/cart').subscribe();

    http.expectOne('/api/v1/cart').flush({ message: 'Требуется вход' }, { status: 401, statusText: 'Unauthorized' });
    http.expectOne('/api/v1/auth/refresh').flush({ user: { id: 'u1' } });
    http.expectOne('/api/v1/cart').flush({ items: [] });
  });

  it('если refresh не удался — наружу уходит исходная 401', (done) => {
    client.get('/api/v1/cart').subscribe({
      error: (err: HttpErrorResponse) => {
        expect(err.status).toBe(401);
        done();
      },
    });

    http.expectOne('/api/v1/cart').flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectOne('/api/v1/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });
  });

  it('не запускает refresh для самого /auth/login', (done) => {
    client.post('/api/v1/auth/login', {}).subscribe({
      error: (err: HttpErrorResponse) => {
        expect(err.status).toBe(401);
        done();
      },
    });

    const req = http.expectOne('/api/v1/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush({ message: 'Неверный логин или пароль' }, { status: 401, statusText: 'Unauthorized' });
    http.expectNone('/api/v1/auth/refresh');
  });

  it('параллельные 401 ждут один общий /auth/refresh', () => {
    client.get('/api/v1/cart').subscribe();
    client.get('/api/v1/me/profile').subscribe();

    http.expectOne('/api/v1/cart').flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectOne('/api/v1/me/profile').flush(null, { status: 401, statusText: 'Unauthorized' });

    const refresh = http.expectOne('/api/v1/auth/refresh');
    http.expectNone('/api/v1/auth/refresh');

    refresh.flush({ user: { id: 'u1' } });
    http.expectOne('/api/v1/cart').flush({ items: [] });
    http.expectOne('/api/v1/me/profile').flush({ fullName: 'Анна' });
  });
});
