import { Order, OrderStatus, Product } from '../../shared/models';

import { mutateDb, readDb } from '../db/mock-db';
import { MockHttpError } from '../mock-error';
import { MockRequestContext, MockResponse, requireRole } from './context';
import { STATUS_TRANSITIONS } from './orders.handlers';

function validateProductDraft(body: any): { fieldErrors: Record<string, string[]>; draft: any } {
  const fieldErrors: Record<string, string[]> = {};
  const draft = { ...body };
  const name = String(body?.name ?? '').trim();
  const price = Number(body?.price);
  const stock = Number(body?.stock);
  const sku = String(body?.sku ?? '').trim();

  if (sku.length < 2) {
    fieldErrors['sku'] = ['Укажите артикул (минимум 2 символа)'];
  }
  if (name.length < 1 || name.length > 300) {
    fieldErrors['name'] = ['Название — от 1 до 300 символов'];
  }
  if (!Number.isFinite(price) || price < 0) {
    fieldErrors['price'] = ['Цена не может быть отрицательной'];
  }
  if (!Number.isFinite(stock) || stock < 0 || !Number.isInteger(stock)) {
    fieldErrors['stock'] = ['Остаток — целое число не меньше нуля'];
  }
  if (!body?.categoryId) {
    fieldErrors['categoryId'] = ['Выберите категорию'];
  }
  draft.name = name;
  draft.sku = sku.toUpperCase();
  return { fieldErrors, draft };
}

function toProductDto(db: ReturnType<typeof readDb>, product: any): Product {
  return {
    ...product,
    category: db.categories.find((c) => c.id === product.categoryId)?.name ?? '',
    brand: product.brandId ? (db.brands.find((b) => b.id === product.brandId)?.name ?? null) : null,
  };
}

export function handleAdminProducts(ctx: MockRequestContext): MockResponse {
  const db = readDb();
  requireRole(ctx, db.users, 'admin');
  const search = (ctx.params.get('search') ?? '').trim().toLowerCase();
  let items = db.products.map((product) => toProductDto(db, product));
  if (search) {
    items = items.filter(
      (product) => product.name.toLowerCase().includes(search) || product.sku.toLowerCase().includes(search),
    );
  }
  return items;
}

export function handleAdminCreateProduct(ctx: MockRequestContext): MockResponse {
  const { fieldErrors, draft } = validateProductDraft(ctx.body);
  if (Object.keys(fieldErrors).length > 0) {
    throw MockHttpError.badRequest('Данные заполнены неверно', fieldErrors);
  }

  return mutateDb((db) => {
    requireRole(ctx, db.users, 'admin');
    if (db.products.some((p) => p.sku === draft.sku)) {
      throw MockHttpError.conflict('Товар с таким артикулом уже существует');
    }
    const product = {
      id: `p-${Date.now().toString(36)}${Math.random().toString(36).slice(2, 6)}`,
      sku: draft.sku,
      name: draft.name,
      description: draft.description ? String(draft.description) : '',
      categoryId: draft.categoryId,
      brandId: draft.brandId ?? null,
      price: draft.price,
      stock: draft.stock,
      imageUrl: draft.imageUrl || 'assets/img/products/korm.svg',
      rating: 0,
      isPopular: false,
      isActive: draft.isActive ?? true,
      createdAt: new Date().toISOString(),
    };
    db.products.push(product);
    return toProductDto(db, product);
  });
}

export function handleAdminUpdateProduct(match: RegExpMatchArray, ctx: MockRequestContext): MockResponse {
  const { fieldErrors, draft } = validateProductDraft(ctx.body);
  if (Object.keys(fieldErrors).length > 0) {
    throw MockHttpError.badRequest('Данные заполнены неверно', fieldErrors);
  }

  return mutateDb((db) => {
    requireRole(ctx, db.users, 'admin');
    const product = db.products.find((p) => p.id === match[1]);
    if (!product) {
      throw MockHttpError.notFound('Товар не найден');
    }
    if (db.products.some((p) => p.id !== product.id && p.sku === draft.sku)) {
      throw MockHttpError.conflict('Товар с таким артикулом уже существует');
    }
    Object.assign(product, {
      sku: draft.sku,
      name: draft.name,
      description: draft.description ? String(draft.description) : '',
      categoryId: draft.categoryId,
      brandId: draft.brandId ?? null,
      price: draft.price,
      stock: draft.stock,
      imageUrl: draft.imageUrl || product.imageUrl,
      isActive: draft.isActive ?? product.isActive,
    });
    return toProductDto(db, product);
  });
}

export function handleAdminDeleteProduct(match: RegExpMatchArray, ctx: MockRequestContext): MockResponse {
  return mutateDb((db) => {
    requireRole(ctx, db.users, 'admin');
    const product = db.products.find((p) => p.id === match[1]);
    if (!product) {
      throw MockHttpError.notFound('Товар не найден');
    }
    db.products = db.products.filter((p) => p.id !== match[1]);
    // Убираем товар из всех корзин.
    for (const cart of Object.values(db.carts)) {
      cart.items = cart.items.filter((item) => item.productId !== match[1]);
    }
    return { status: 204, body: null };
  });
}

export function handleAdminOrders(ctx: MockRequestContext): MockResponse {
  const db = readDb();
  requireRole(ctx, db.users, 'admin');
  const status = ctx.params.get('status') as OrderStatus | null;
  let orders: Order[] = [...db.orders].sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  if (status) {
    orders = orders.filter((order) => order.status === status);
  }
  return orders;
}

export function handleAdminOrderStatus(match: RegExpMatchArray, ctx: MockRequestContext): MockResponse {
  return mutateDb((db) => {
    requireRole(ctx, db.users, 'admin');
    const order = db.orders.find((o) => o.id === match[1]);
    if (!order) {
      throw MockHttpError.notFound('Заказ не найден');
    }
    const next = ctx.body?.status as OrderStatus;
    if (!['new', 'confirmed', 'delivered', 'cancelled'].includes(next)) {
      throw MockHttpError.badRequest('Данные заполнены неверно', { status: ['Неизвестный статус'] });
    }
    if (!STATUS_TRANSITIONS[order.status].includes(next)) {
      throw MockHttpError.conflict('Недопустимый переход статуса заказа');
    }

    if (next === 'cancelled') {
      // Возврат остатков на склад при отмене.
      for (const item of order.items) {
        if (!item.productId) {
          continue;
        }
        const product = db.products.find((p) => p.id === item.productId);
        if (product) {
          product.stock += item.quantity;
        }
      }
    }
    order.status = next;
    return order;
  });
}

export function handleAdminUsers(ctx: MockRequestContext): MockResponse {
  const db = readDb();
  requireRole(ctx, db.users, 'admin');
  return db.users.map((user) => ({
    id: user.id,
    login: user.login,
    fullName: user.fullName,
    email: user.email,
    phone: user.phone,
    role: user.role,
    createdAt: user.createdAt,
  }));
}
