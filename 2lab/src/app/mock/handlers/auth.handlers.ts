import { PublicUser, RecoveryChannel } from '../../shared/models';
import { EMAIL_PATTERN, normalizePhone, passwordIssues } from '../../shared/validators/validators';

import { mutateDb, readDb, setSessionUserId } from '../db/mock-db';
import { MockHttpError } from '../mock-error';
import { MockRequestContext, MockResponse } from './context';

/** Тест-триггеры демо-режима (задокументированы в README). */
export const TRIGGER_SLOW = 'медленно';
export const TRIGGER_ERROR = 'сбой';

/** Ограничение частоты входа: не более 5 попыток в минуту на пару (login, IP). */
const loginAttempts = new Map<string, number[]>();
const RATE_LIMIT = 5;
const RATE_WINDOW_MS = 60_000;

/** Только для unit-тестов: сбрасывает счётчики rate-limit. */
export function resetLoginRateLimit(): void {
  loginAttempts.clear();
}

function publicUser(user: any): PublicUser {
  return {
    id: user.id,
    login: user.login,
    fullName: user.fullName,
    email: user.email,
    phone: user.phone,
    role: user.role,
  };
}

function detectChannel(contact: string): RecoveryChannel {
  return contact.includes('@') ? 'email' : 'phone';
}

function maskContact(contact: string, channel: RecoveryChannel): string {
  if (channel === 'email') {
    const [name, domain] = contact.split('@');
    const visible = name.slice(0, Math.min(3, name.length));
    return `${visible}${'•'.repeat(Math.max(3, name.length - visible.length))}@${domain}`;
  }
  const normalized = normalizePhone(contact);
  return `${normalized.slice(0, 7)} ***-**-${normalized.slice(-2)}`;
}

export function handleLogin(ctx: MockRequestContext): MockResponse {
  const login = String(ctx.body?.login ?? '').trim();
  const password = String(ctx.body?.password ?? '');

  // Тест-триггеры: «медленно» — доп. задержка, «сбой» — имитация 500-й ошибки сервера.
  if (login.toLowerCase().includes(TRIGGER_SLOW)) {
    ctx.extraDelayMs += 2000;
  }
  if (login.toLowerCase().includes(TRIGGER_ERROR)) {
    throw MockHttpError.server();
  }

  const key = `${login.toLowerCase()}@local`;
  const now = Date.now();
  const attempts = (loginAttempts.get(key) ?? []).filter((t) => now - t < RATE_WINDOW_MS);
  if (attempts.length >= RATE_LIMIT) {
    loginAttempts.set(key, attempts);
    throw MockHttpError.tooManyRequests('Слишком много попыток входа. Попробуйте через минуту');
  }
  attempts.push(now);
  loginAttempts.set(key, attempts);

  const db = readDb();
  const user = db.users.find(
    (u) =>
      u.login.toLowerCase() === login.toLowerCase() ||
      u.email.toLowerCase() === login.toLowerCase() ||
      (u.phone !== null && normalizePhone(login) === u.phone),
  );

  // Единое сообщение — не раскрываем существование аккаунта.
  if (!user || user.password !== password) {
    throw MockHttpError.unauthorized('Неверный логин или пароль');
  }

  setSessionUserId(user.id);
  return { user: publicUser(user) };
}

export function handleLogout(): MockResponse {
  setSessionUserId(null);
  return { status: 204, body: null };
}

export function handleMe(ctx: MockRequestContext): MockResponse {
  const db = readDb();
  const user = db.users.find((u) => u.id === ctx.sessionUserId);
  if (!user) {
    throw MockHttpError.unauthorized();
  }
  return publicUser(user);
}

export function handleRecoveryRequest(ctx: MockRequestContext): MockResponse {
  const rawContact = String(ctx.body?.contact ?? '').trim();
  const channel = detectChannel(rawContact);
  const contact = channel === 'email' ? rawContact.toLowerCase() : normalizePhone(rawContact);

  if (channel === 'email' && !EMAIL_PATTERN.test(contact)) {
    throw MockHttpError.badRequest('Данные заполнены неверно', { contact: ['Введите корректный email'] });
  }
  if (channel === 'phone' && !/^\+7\d{10}$/.test(contact)) {
    throw MockHttpError.badRequest('Данные заполнены неверно', { contact: ['Введите корректный номер телефона'] });
  }

  const db = readDb();
  const user = db.users.find(
    (u) => (channel === 'email' && u.email.toLowerCase() === contact) || (channel === 'phone' && u.phone === contact),
  );

  // Всегда отвечаем успехом — не раскрываем существование контакта.
  const maskedContact = maskContact(contact, channel);
  if (!user) {
    return { channel, maskedContact };
  }

  const code = String(Math.floor(100000 + Math.random() * 900000));
  const expiresAt = new Date(Date.now() + 10 * 60 * 1000).toISOString();
  mutateDb((draft) => {
    draft.recoveryRequests = draft.recoveryRequests.filter((r) => r.contact !== contact);
    draft.recoveryRequests.push({
      id: `rr-${Date.now().toString(36)}`,
      userId: user.id,
      contact,
      channel,
      code,
      expiresAt,
      attempts: 0,
      used: false,
      createdAt: new Date().toISOString(),
    });
  });

  // Демо-режим без SMTP/SMS: код восстановления выводится в консоль браузера.
  console.info(
    `%c[Муркет · демо]%c Код восстановления для ${maskedContact}: %c${code}`,
    'color:#3D5A3C;font-weight:700',
    'color:inherit',
    'color:#E07B39;font-weight:700;font-size:14px',
  );

  return { channel, maskedContact };
}

export function handleRecoveryConfirm(ctx: MockRequestContext): MockResponse {
  const rawContact = String(ctx.body?.contact ?? '').trim();
  const code = String(ctx.body?.code ?? '').trim();
  const channel = detectChannel(rawContact);
  const contact = channel === 'email' ? rawContact.toLowerCase() : normalizePhone(rawContact);

  return mutateDb((db) => {
    const request = db.recoveryRequests.find((r) => r.contact === contact && !r.used);
    if (!request) {
      throw MockHttpError.badRequest('Код восстановления не подходит');
    }
    if (new Date(request.expiresAt).getTime() < Date.now()) {
      throw MockHttpError.badRequest('Код восстановления не подходит');
    }
    if (request.code !== code) {
      request.attempts += 1;
      if (request.attempts >= 5) {
        request.used = true;
      }
      throw MockHttpError.badRequest('Код восстановления не подходит');
    }

    request.used = true;
    const token = `rt_${Date.now().toString(36)}${Math.random().toString(36).slice(2, 10)}`;
    db.resetTokens = db.resetTokens.filter((t) => t.userId !== request.userId);
    db.resetTokens.push({ token, userId: request.userId, expiresAt: new Date(Date.now() + 15 * 60 * 1000).toISOString() });
    return { resetToken: token };
  });
}

export function handleResetPassword(ctx: MockRequestContext): MockResponse {
  const token = String(ctx.body?.resetToken ?? '');
  const password = String(ctx.body?.password ?? '');

  return mutateDb((db) => {
    const record = db.resetTokens.find((t) => t.token === token);
    if (!record || new Date(record.expiresAt).getTime() < Date.now()) {
      throw MockHttpError.badRequest('Ссылка восстановления недействительна или истекла');
    }
    const issues = passwordIssues(password);
    if (issues.length > 0) {
      throw MockHttpError.badRequest('Данные заполнены неверно', {
        password: issues.map((issue) => `Пароль: ${issue}`),
      });
    }
    const user = db.users.find((u) => u.id === record.userId);
    if (!user) {
      throw MockHttpError.badRequest('Ссылка восстановления недействительна или истекла');
    }
    user.password = password;
    db.resetTokens = db.resetTokens.filter((t) => t.userId !== record.userId);
    return { ok: true };
  });
}
