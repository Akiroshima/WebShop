import { InjectionToken } from '@angular/core';

/**
 * Фиксированная задержка мок-ответа в мс.
 * По умолчанию −1 — случайная задержка 500–800 мс, как у «настоящего» сервера.
 * В unit-тестах переопределяется на 0.
 */
export const MOCK_DELAY_MS = new InjectionToken<number>('MOCK_DELAY_MS', {
  providedIn: 'root',
  factory: () => -1,
});
