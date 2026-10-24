import { CheckoutPayload, CheckoutResult, DELIVERY_OPTIONS, Order, OrderStatus } from '../../shared/models';

import { MockDb, mutateDb, readDb } from '../db/mock-db';
import { findPromoByCode, promoDiscount, promoErrorMessage, round2 } from '../db/promo';
import { MockHttpError } from '../mock-error';
import { MockRequestContext, MockResponse, requireUser } from './context';
import { cartStateFor } from './cart.handlers';

export const MAX_ADDRESSES_PER_USER = 10;

const STATUS_TRANSITIONS: Record<OrderStatus, OrderStatus[]> = {
  new: ['confirmed', 'cancelled'],
  confirmed: ['delivered', 'cancelled'],
  delivered: [],
  cancelled: [],
};

function requireOwnOrder(db: MockDb, ctx: MockRequestContext, orderId: string): Order {
  const user = requireUser(ctx, db.users);
  const order = db.orders.find((o) => o.id === orderId);
  // Чужие заказы — 404: существование не раскрывается.
  if (!order || order.userId !== user.id) {
    throw MockHttpError.notFound('Заказ не найден');
  }
  return order;
}

export function handleCreateOrder(ctx: MockRequestContext): MockResponse {
  const payload = ctx.body as CheckoutPayload;
  const fieldErrors: Record<string, string[]> = {};
  if (!payload?.recipientName?.trim()) {
    fieldErrors['recipientName'] = ['Укажите имя получателя'];
  }
  if (!payload?.recipientPhone?.trim()) {
    fieldErrors['recipientPhone'] = ['Укажите телефон'];
  }
  if (!payload?.address?.city?.trim() || !payload?.address?.street?.trim() || !payload?.address?.house?.trim()) {
    fieldErrors['address'] = ['Укажите город, улицу и дом'];
  }
  if (Object.keys(fieldErrors).length > 0) {
    throw MockHttpError.badRequest('Данные заполнены неверно', fieldErrors);
  }

  return mutateDb((db) => {
    const user = requireUser(ctx, db.users);
    const cart = db.carts[user.id];
    const state = cartStateFor(db, user.id);
    if (!cart || state.items.length === 0) {
      throw MockHttpError.conflict('Корзина пуста');
    }

    // Повторная валидация остатков.
    for (const item of state.items) {
      const product = db.products.find((p) => p.id === item.productId);
      if (!product || !product.isActive) {
        throw MockHttpError.conflict('Некоторые товары стали недоступны');
      }
      if (item.quantity > product.stock) {
        throw MockHttpError.conflict(`В наличии только ${product.stock} шт. товара «${product.name}»`);
      }
    }

    // Повторная валидация промокода.
    let promoCode: string | null = null;
    if (cart.promoCode) {
      const promo = findPromoByCode(db.promos, cart.promoCode);
      if (!promo || promoErrorMessage(promo) || state.subtotal < promo.minSubtotal) {
        cart.promoCode = null;
        throw MockHttpError.conflict('Промокод больше не действует');
      }
      promoCode = promo.code;
      promo.usedCount += 1;
    }

    if (payload.saveAddress && db.addresses.filter((a) => a.userId === user.id).length >= MAX_ADDRESSES_PER_USER) {
      throw MockHttpError.conflict(`Нельзя сохранить больше ${MAX_ADDRESSES_PER_USER} адресов`);
    }

    const subtotal = state.subtotal;
    const discount = promoCode ? state.discount : 0;
    const deliveryOption = DELIVERY_OPTIONS.find((option) => option.value === payload.deliveryMethod) ?? DELIVERY_OPTIONS[0];
    const total = round2(subtotal - discount + deliveryOption.price);

    const order: Order = {
      id: `o-${Date.now().toString(36)}${Math.random().toString(36).slice(2, 6)}`,
      number: db.nextOrderNumber++,
      userId: user.id,
      status: 'new',
      createdAt: new Date().toISOString(),
      recipientName: payload.recipientName.trim(),
      recipientPhone: payload.recipientPhone,
      recipientEmail: payload.recipientEmail ?? user.email,
      address: { ...payload.address },
      comment: payload.comment?.trim() || null,
      promoCode,
      subtotal,
      discount,
      deliveryMethod: deliveryOption.value,
      delivery: deliveryOption.price,
      total,
      // Снимки товаров на момент оформления.
      items: state.items.map((item) => ({
        productId: item.productId,
        name: item.name,
        sku: item.sku,
        price: item.price,
        quantity: item.quantity,
      })),
    };
    db.orders.push(order);

    // Списание остатков — атомарно в рамках одного обновления мок-БД.
    for (const item of order.items) {
      const product = db.products.find((p) => p.id === item.productId);
      if (product) {
        product.stock = Math.max(0, product.stock - item.quantity);
      }
    }

    if (payload.saveAddress) {
      const defaultExisted = db.addresses.some((a) => a.userId === user.id && a.isDefault);
      db.addresses.push({
        id: `a-${Date.now().toString(36)}${Math.random().toString(36).slice(2, 6)}`,
        userId: user.id,
        label: null,
        recipient: order.recipientName,
        phone: order.recipientPhone,
        city: order.address.city,
        street: order.address.street,
        house: order.address.house,
        apartment: order.address.apartment,
        entrance: order.address.entrance,
        floor: order.address.floor,
        intercom: order.address.intercom,
        postalCode: order.address.postalCode,
        isDefault: !defaultExisted,
      });
    }

    // Корзина очищается после оформления.
    cart.items = [];
    cart.promoCode = null;

    const result: CheckoutResult = { id: order.id, number: order.number };
    return result;
  });
}

export function handleMyOrders(ctx: MockRequestContext): MockResponse {
  const db = readDb();
  const user = requireUser(ctx, db.users);
  return db.orders
    .filter((order) => order.userId === user.id)
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
}

export function handleMyOrderDetail(match: RegExpMatchArray, ctx: MockRequestContext): MockResponse {
  const db = readDb();
  return requireOwnOrder(db, ctx, match[1]);
}

export { STATUS_TRANSITIONS, requireOwnOrder };
