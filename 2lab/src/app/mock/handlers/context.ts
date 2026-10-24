import { DbUser } from '../../shared/models';

import { MockHttpError } from '../mock-error';

export interface MockRequestContext {
  method: string;
  /** Путь без query: /api/v1/... */
  path: string;
  params: URLSearchParams;
  body: any;
  /** Пользователь из ключа сессии mock.session.userId. */
  sessionUserId: string | null;
  /** Дополнительная задержка поверх стандартной (тест-триггеры). */
  extraDelayMs: number;
}

export type MockResponse = { status: number; body?: unknown } | unknown;

export function requireUser(ctx: MockRequestContext, dbUsers: DbUser[]): DbUser {
  if (!ctx.sessionUserId) {
    throw MockHttpError.unauthorized();
  }
  const user = dbUsers.find((u) => u.id === ctx.sessionUserId);
  if (!user) {
    throw MockHttpError.unauthorized('Сессия недействительна. Войдите снова');
  }
  return user;
}

export function requireRole(ctx: MockRequestContext, dbUsers: DbUser[], role: DbUser['role']): DbUser {
  const user = requireUser(ctx, dbUsers);
  if (user.role !== role) {
    throw MockHttpError.forbidden();
  }
  return user;
}
