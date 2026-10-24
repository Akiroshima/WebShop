import { Address, DbCart, DbUser, Order, Promo } from '../../shared/models';
import { Brand, Category, Product } from '../../shared/models/catalog';

import { CUSTOMER_ID, SEED_VERSION, seedDb } from './seed';

export interface DbProduct extends Omit<Product, 'category' | 'brand'> {}

export interface RecoveryRequest {
  id: string;
  userId: string;
  contact: string;
  channel: 'email' | 'phone';
  code: string;
  expiresAt: string;
  attempts: number;
  used: boolean;
  createdAt: string;
}

export interface ResetToken {
  token: string;
  userId: string;
  expiresAt: string;
}

export interface MockDb {
  seedVersion: number;
  users: DbUser[];
  categories: Category[];
  brands: Brand[];
  products: DbProduct[];
  addresses: Address[];
  promos: Promo[];
  carts: Record<string, DbCart>;
  orders: Order[];
  nextOrderNumber: number;
  recoveryRequests: RecoveryRequest[];
  resetTokens: ResetToken[];
}

export const DB_KEY = 'mock.db.v1';
export const SESSION_KEY = 'mock.session.userId';

function persist(db: MockDb): MockDb {
  try {
    localStorage.setItem(DB_KEY, JSON.stringify(db));
  } catch {
    // Хранилище недоступно (приватный режим) — работаем в памяти.
  }
  return db;
}

/** Загружает мок-БД; при отсутствии или повреждении пересоздаёт сид. */
export function loadDb(): MockDb {
  try {
    const raw = localStorage.getItem(DB_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as MockDb;
      if (parsed && parsed.seedVersion === SEED_VERSION && Array.isArray(parsed.users)) {
        return parsed;
      }
    }
  } catch {
    // Повреждённые данные — пересоздаём.
  }
  return persist(seedDb());
}

export function readDb(): MockDb {
  return loadDb();
}

/** Мутирует копию БД и сохраняет её в localStorage. */
export function mutateDb<T>(mutate: (db: MockDb) => T): T {
  const db = loadDb();
  const result = mutate(db);
  persist(db);
  return result;
}

/** Возвращает мок-БД к исходному состоянию (кнопка «сброса» в демо). */
export function resetDb(): MockDb {
  return persist(seedDb());
}

export function getSessionUserId(): string | null {
  try {
    return localStorage.getItem(SESSION_KEY);
  } catch {
    return null;
  }
}

export function setSessionUserId(userId: string | null): void {
  try {
    if (userId) {
      localStorage.setItem(SESSION_KEY, userId);
    } else {
      localStorage.removeItem(SESSION_KEY);
    }
  } catch {
    // Игнорируем недоступное хранилище.
  }
}

export { CUSTOMER_ID, SEED_VERSION, seedDb };
