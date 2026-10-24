import { Address, AddressDraft, Profile } from '../../shared/models';
import { EMAIL_PATTERN, isValidPhone, normalizePhone, passwordIssues } from '../../shared/validators/validators';

import { mutateDb, readDb } from '../db/mock-db';
import { MockHttpError } from '../mock-error';
import { MockRequestContext, MockResponse, requireUser } from './context';
import { MAX_ADDRESSES_PER_USER } from './orders.handlers';

function validateProfileDraft(body: any): { fieldErrors: Record<string, string[]>; normalized: Profile } {
  const fieldErrors: Record<string, string[]> = {};
  const fullName = String(body?.fullName ?? '').trim();
  const email = String(body?.email ?? '').trim().toLowerCase();
  const phone = body?.phone ? normalizePhone(String(body.phone)) : null;

  if (!fullName) {
    fieldErrors['fullName'] = ['Укажите имя и фамилию'];
  }
  if (!EMAIL_PATTERN.test(email)) {
    fieldErrors['email'] = ['Введите корректный email'];
  }
  if (phone && !isValidPhone(phone)) {
    fieldErrors['phone'] = ['Введите корректный номер телефона'];
  }
  return { fieldErrors, normalized: { fullName, email, phone } };
}

export function handleGetProfile(ctx: MockRequestContext): MockResponse {
  const db = readDb();
  const user = requireUser(ctx, db.users);
  return { fullName: user.fullName, email: user.email, phone: user.phone } satisfies Profile;
}

export function handleUpdateProfile(ctx: MockRequestContext): MockResponse {
  const { fieldErrors, normalized } = validateProfileDraft(ctx.body);
  if (Object.keys(fieldErrors).length > 0) {
    throw MockHttpError.badRequest('Данные заполнены неверно', fieldErrors);
  }

  return mutateDb((db) => {
    const user = requireUser(ctx, db.users);
    if (db.users.some((u) => u.id !== user.id && u.email.toLowerCase() === normalized.email)) {
      throw MockHttpError.conflict('Пользователь с таким email уже существует');
    }
    if (normalized.phone && db.users.some((u) => u.id !== user.id && u.phone === normalized.phone)) {
      throw MockHttpError.conflict('Пользователь с таким телефоном уже существует');
    }
    user.fullName = normalized.fullName;
    user.email = normalized.email;
    user.phone = normalized.phone;
    return { fullName: user.fullName, email: user.email, phone: user.phone } satisfies Profile;
  });
}

export function handleChangePassword(ctx: MockRequestContext): MockResponse {
  return mutateDb((db) => {
    const user = requireUser(ctx, db.users);
    const currentPassword = String(ctx.body?.currentPassword ?? '');
    const password = String(ctx.body?.password ?? '');

    if (user.password !== currentPassword) {
      throw MockHttpError.badRequest('Неверный текущий пароль');
    }
    const issues = passwordIssues(password);
    if (issues.length > 0) {
      throw MockHttpError.badRequest('Данные заполнены неверно', { password: issues.map((i) => `Пароль: ${i}`) });
    }
    user.password = password;
    return { ok: true };
  });
}

export function handleListAddresses(ctx: MockRequestContext): MockResponse {
  const db = readDb();
  const user = requireUser(ctx, db.users);
  return db.addresses.filter((address) => address.userId === user.id);
}

function validateAddressDraft(body: AddressDraft | any): { fieldErrors: Record<string, string[]>; draft: AddressDraft } {
  const fieldErrors: Record<string, string[]> = {};
  const draft: AddressDraft = {
    label: body?.label ? String(body.label).trim() : null,
    recipient: String(body?.recipient ?? '').trim(),
    phone: body?.phone ? normalizePhone(String(body.phone)) : '',
    city: String(body?.city ?? '').trim(),
    street: String(body?.street ?? '').trim(),
    house: String(body?.house ?? '').trim(),
    apartment: body?.apartment ? String(body.apartment).trim() : null,
    entrance: body?.entrance ? String(body.entrance).trim() : null,
    floor: body?.floor ? String(body.floor).trim() : null,
    intercom: body?.intercom ? String(body.intercom).trim() : null,
    postalCode: body?.postalCode ? String(body.postalCode).trim() : null,
    isDefault: Boolean(body?.isDefault),
  };
  if (!draft.recipient) {
    fieldErrors['recipient'] = ['Укажите получателя'];
  }
  if (!draft.phone || !isValidPhone(draft.phone)) {
    fieldErrors['phone'] = ['Введите корректный номер телефона'];
  }
  if (!draft.city) {
    fieldErrors['city'] = ['Укажите город'];
  }
  if (!draft.street) {
    fieldErrors['street'] = ['Укажите улицу'];
  }
  if (!draft.house) {
    fieldErrors['house'] = ['Укажите дом'];
  }
  return { fieldErrors, draft };
}

export function handleCreateAddress(ctx: MockRequestContext): MockResponse {
  const { fieldErrors, draft } = validateAddressDraft(ctx.body);
  if (Object.keys(fieldErrors).length > 0) {
    throw MockHttpError.badRequest('Данные заполнены неверно', fieldErrors);
  }

  return mutateDb((db) => {
    const user = requireUser(ctx, db.users);
    const own = db.addresses.filter((a) => a.userId === user.id);
    if (own.length >= MAX_ADDRESSES_PER_USER) {
      throw MockHttpError.conflict(`Нельзя сохранить больше ${MAX_ADDRESSES_PER_USER} адресов`);
    }
    const address: Address = {
      id: `a-${Date.now().toString(36)}${Math.random().toString(36).slice(2, 6)}`,
      userId: user.id,
      ...draft,
    };
    if (address.isDefault) {
      for (const other of own) {
        other.isDefault = false;
      }
    }
    db.addresses.push(address);
    return address;
  });
}

export function handleUpdateAddress(match: RegExpMatchArray, ctx: MockRequestContext): MockResponse {
  const { fieldErrors, draft } = validateAddressDraft(ctx.body);
  if (Object.keys(fieldErrors).length > 0) {
    throw MockHttpError.badRequest('Данные заполнены неверно', fieldErrors);
  }

  return mutateDb((db) => {
    const user = requireUser(ctx, db.users);
    const address = db.addresses.find((a) => a.id === match[1] && a.userId === user.id);
    if (!address) {
      throw MockHttpError.notFound('Адрес не найден');
    }
    Object.assign(address, draft);
    if (address.isDefault) {
      for (const other of db.addresses) {
        if (other.userId === user.id && other.id !== address.id) {
          other.isDefault = false;
        }
      }
    }
    return address;
  });
}

export function handleDeleteAddress(match: RegExpMatchArray, ctx: MockRequestContext): MockResponse {
  return mutateDb((db) => {
    const user = requireUser(ctx, db.users);
    const address = db.addresses.find((a) => a.id === match[1] && a.userId === user.id);
    if (!address) {
      throw MockHttpError.notFound('Адрес не найден');
    }
    db.addresses = db.addresses.filter((a) => a.id !== match[1]);
    // Удаление адреса не влияет на оформленные заказы — в них хранится снимок.
    return { status: 204, body: null };
  });
}
