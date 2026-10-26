import { HttpClient, HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, finalize, share, switchMap, throwError } from 'rxjs';

import { API_BASE_URL } from '../config/api';

/**
 * Интерцептор сессии: при 401 (истёк access-cookie) один раз вызывает POST /auth/refresh
 * и повторяет исходный запрос. Для эндпоинтов самого /auth/* обновление не запускается.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Функциональный интерцептор выполняется в DI-контексте — HttpClient берём здесь.
  const http = inject(HttpClient);

  if (isAuthRequest(req.url)) {
    return next(req);
  }

  return next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      return refreshSession(http).pipe(
        switchMap(() => next(req)),
        catchError(() => throwError(() => error)),
      );
    }),
  );
};

/** Эти запросы сами управляют сессией — refresh для них не запускаем (иначе цикл). */
function isAuthRequest(url: string): boolean {
  const base = `${API_BASE_URL}/auth`;
  return (
    url === `${base}/login` ||
    url === `${base}/logout` ||
    url === `${base}/refresh` ||
    url.startsWith(`${base}/recovery`) ||
    url === `${base}/reset-password`
  );
}

let refreshInFlight: Observable<unknown> | null = null;

/** Общий запрос /auth/refresh: параллельные 401 ждут одно обновление, а не устраивают гонку. */
function refreshSession(http: HttpClient): Observable<unknown> {
  if (!refreshInFlight) {
    refreshInFlight = http.post(`${API_BASE_URL}/auth/refresh`, {}).pipe(
      finalize(() => (refreshInFlight = null)),
      share(),
    );
  }
  return refreshInFlight;
}

/** Только для тестов: сброс общего in-flight запроса. */
export function resetRefreshInFlight(): void {
  refreshInFlight = null;
}
