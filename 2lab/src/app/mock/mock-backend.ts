import { HttpBackend, HttpRequest, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { HttpErrorResponse } from '@angular/common/http';

import { getSessionUserId } from './db/mock-db';
import { dispatch } from './handlers';
import { MockHttpError } from './mock-error';
import { MOCK_DELAY_MS } from './tokens';

/**
 * Браузерный мок-бэкенд: подменяет HttpBackend, поэтому приложение работает
 * через обычный HttpClient, а все данные обслуживает мок-БД в localStorage.
 */
@Injectable()
export class MockBackend implements HttpBackend {
  private readonly fixedDelayMs = inject(MOCK_DELAY_MS);

  handle(req: HttpRequest<unknown>): Observable<HttpResponse<unknown>> {
    // urlWithParams включает query-параметры из HttpClient-опции {params} (req.url их не содержит).
    const url = new URL(req.urlWithParams, 'http://mock.local');
    const ctx = {
      method: req.method,
      path: url.pathname.replace(/\/+$/, '') || '/',
      params: url.searchParams,
      body: req.body,
      sessionUserId: getSessionUserId(),
      extraDelayMs: 0,
    };

    const delay = this.fixedDelayMs >= 0 ? this.fixedDelayMs : 500 + Math.floor(Math.random() * 301);

    return new Observable<HttpResponse<unknown>>((subscriber) => {
      const timer = setTimeout(() => {
        try {
          const result = dispatch(ctx);
          if (result && typeof result === 'object' && 'status' in result && typeof (result as any).status === 'number') {
            const wrapped = result as { status: number; body?: unknown };
            subscriber.next(new HttpResponse({ status: wrapped.status, body: wrapped.body ?? null }));
          } else {
            subscriber.next(new HttpResponse({ status: 200, body: result ?? null }));
          }
          subscriber.complete();
        } catch (error) {
          if (error instanceof MockHttpError) {
            subscriber.error(
              new HttpErrorResponse({
                status: error.status,
                statusText: 'Mock Error',
                error: { message: error.message, errors: error.fieldErrors },
              }),
            );
          } else {
            console.error('[Муркет · мок-слой] Необработанная ошибка:', error);
            subscriber.error(
              new HttpErrorResponse({
                status: 500,
                statusText: 'Mock Error',
                error: { message: 'Внутренняя ошибка сервера. Попробуйте позже' },
              }),
            );
          }
        }
      }, delay + ctx.extraDelayMs);

      // Отмена запроса (например, при уходе со страницы) — останавливаем таймер.
      return () => clearTimeout(timer);
    });
  }
}
