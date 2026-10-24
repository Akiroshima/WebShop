import { AppliedPromo, CartItem, CartMutationResult, CartState, DbCart } from '../../shared/models';

import { DbProduct, MockDb, mutateDb, readDb } from '../db/mock-db';
import { findPromoByCode, promoDiscount, promoErrorMessage, round2 } from '../db/promo';
import { MockHttpError } from '../mock-error';
import { MockRequestContext, MockResponse, requireUser } from './context';

/** Тест-триггер демо-режима: промокод «СБОЙ500» имитирует 500-ю ошибку сервера. */
export const TRIGGER_PROMO_ERROR = 'СБОЙ500';

function emptyCart(): CartState {
  return { items: [], itemsCount: 0, subtotal: 0, promo: null, discount: 0, total: 0 };
}

function getOrCreateCart(db: MockDb, userId: string): DbCart {
  if (!db.carts[userId]) {
    db.carts[userId] = { userId, promoCode: null, items: [] };
  }
  return db.carts[userId];
}

function buildCartState(db: MockDb, userId: string): CartState {
  const cart = db.carts[userId];
  if (!cart || cart.items.length === 0) {
    // Промокод на пустой корзине не удерживается.
    if (cart) {
      cart.promoCode = null;
    }
    return emptyCart();
  }

  const items: CartItem[] = [];
  for (const entry of [...cart.items].sort((a, b) => a.addedAt.localeCompare(b.addedAt))) {
    const product = db.products.find((p) => p.id === entry.productId);
    if (!product || !product.isActive) {
      continue;
    }
    const brand = product.brandId ? (db.brands.find((b) => b.id === product.brandId)?.name ?? null) : null;
    items.push({
      productId: product.id,
      name: product.name,
      sku: product.sku,
      brand,
      price: product.price, // цены в корзине — всегда текущие
      stock: product.stock,
      imageUrl: product.imageUrl,
      quantity: entry.quantity,
      subtotal: round2(product.price * entry.quantity),
    });
  }
  if (items.length === 0) {
    return emptyCart();
  }

  const subtotal = round2(items.reduce((sum, item) => sum + item.subtotal, 0));
  let discount = 0;
  let promo: AppliedPromo | null = null;

  if (cart.promoCode) {
    const promoRecord = findPromoByCode(db.promos, cart.promoCode);
    const usable = promoRecord && !promoErrorMessage(promoRecord) && subtotal >= promoRecord.minSubtotal;
    if (usable && promoRecord) {
      discount = promoDiscount(promoRecord, subtotal);
      promo = { code: promoRecord.code, type: promoRecord.type, value: promoRecord.value, discount };
    } else {
      cart.promoCode = null;
    }
  }

  return {
    items,
    itemsCount: items.reduce((sum, item) => sum + item.quantity, 0),
    subtotal,
    promo,
    discount,
    total: round2(subtotal - discount),
  };
}

export function handleGetCart(ctx: MockRequestContext): MockResponse {
  return mutateDb((db) => {
    const user = requireUser(ctx, db.users);
    return buildCartState(db, user.id);
  });
}

export function handleAddItem(ctx: MockRequestContext): MockResponse {
  return mutateDb((db) => {
    const user = requireUser(ctx, db.users);
    const productId = String(ctx.body?.productId ?? '');
    const quantity = Math.max(1, Number(ctx.body?.quantity ?? 1) || 1);

    const product = db.products.find((p) => p.id === productId);
    if (!product || !product.isActive || product.stock <= 0) {
      throw MockHttpError.conflict('Товара нет в наличии');
    }

    const cart = getOrCreateCart(db, user.id);
    const existing = cart.items.find((item) => item.productId === productId);
    const current = existing?.quantity ?? 0;
    const target = current + quantity;

    let notice: string | undefined;
    let finalQuantity = target;
    if (target > product.stock) {
      finalQuantity = product.stock;
      notice = `В наличии только ${product.stock} шт.`;
    }

    if (existing) {
      existing.quantity = finalQuantity;
      existing.addedAt = new Date().toISOString();
    } else {
      cart.items.push({ productId, quantity: finalQuantity, addedAt: new Date().toISOString() });
    }

    const result: CartMutationResult = { cart: buildCartState(db, user.id), notice };
    return result;
  });
}

export function handleUpdateItemQty(match: RegExpMatchArray, ctx: MockRequestContext): MockResponse {
  return mutateDb((db) => {
    const user = requireUser(ctx, db.users);
    const productId = match[1];
    const quantity = Number(ctx.body?.quantity);

    const cart = db.carts[user.id];
    const item = cart?.items.find((entry) => entry.productId === productId);
    if (!cart || !item) {
      throw MockHttpError.notFound('Товар в корзине не найден');
    }
    if (!Number.isFinite(quantity)) {
      throw MockHttpError.badRequest('Данные заполнены неверно', { quantity: ['Укажите количество'] });
    }

    const product = db.products.find((p) => p.id === productId);
    if (!product) {
      throw MockHttpError.notFound('Товар в корзине не найден');
    }

    if (quantity <= 0) {
      cart.items = cart.items.filter((entry) => entry.productId !== productId);
      return { cart: buildCartState(db, user.id) } satisfies CartMutationResult;
    }

    if (quantity > product.stock) {
      throw MockHttpError.conflict(`В наличии только ${product.stock} шт.`);
    }
    item.quantity = quantity;

    return { cart: buildCartState(db, user.id) } satisfies CartMutationResult;
  });
}

export function handleRemoveItem(match: RegExpMatchArray, ctx: MockRequestContext): MockResponse {
  return mutateDb((db) => {
    const user = requireUser(ctx, db.users);
    const cart = db.carts[user.id];
    if (cart) {
      cart.items = cart.items.filter((entry) => entry.productId !== match[1]);
    }
    return { cart: buildCartState(db, user.id) } satisfies CartMutationResult;
  });
}

export function handleApplyPromo(ctx: MockRequestContext): MockResponse {
  const code = String(ctx.body?.code ?? '').trim();
  if (code.toUpperCase() === TRIGGER_PROMO_ERROR) {
    throw MockHttpError.server();
  }

  return mutateDb((db) => {
    const user = requireUser(ctx, db.users);
    const cart = getOrCreateCart(db, user.id);
    if (cart.items.length === 0) {
      throw MockHttpError.conflict('Корзина пуста — промокод применить нельзя');
    }

    const promo = findPromoByCode(db.promos, code);
    if (!promo) {
      throw MockHttpError.conflict('Промокод не найден или не действует');
    }
    const error = promoErrorMessage(promo);
    if (error) {
      throw MockHttpError.conflict(error);
    }

    const state = buildCartState(db, user.id);
    if (state.subtotal < promo.minSubtotal) {
      throw MockHttpError.conflict('Сумма заказа меньше минимальной для этого промокода');
    }

    cart.promoCode = promo.code;
    const result: CartMutationResult = { cart: buildCartState(db, user.id), notice: 'Промокод применён' };
    return result;
  });
}

export function handleRemovePromo(ctx: MockRequestContext): MockResponse {
  return mutateDb((db) => {
    const user = requireUser(ctx, db.users);
    const cart = getOrCreateCart(db, user.id);
    cart.promoCode = null;
    return { cart: buildCartState(db, user.id) } satisfies CartMutationResult;
  });
}

/** Общая функция для обработчиков заказов и корзины. */
export function cartStateFor(db: MockDb, userId: string): CartState {
  return buildCartState(db, userId);
}

export function findProduct(db: MockDb, productId: string): DbProduct | undefined {
  return db.products.find((p) => p.id === productId);
}
