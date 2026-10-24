import { CatalogSort, CategoryWithCount, BrandWithCount, Paged, Product } from '../../shared/models';

import { DbProduct, readDb } from '../db/mock-db';
import { MockHttpError } from '../mock-error';
import { MockRequestContext, MockResponse } from './context';

export const CATALOG_PAGE_SIZE = 12;

function toDto(product: DbProduct, db: ReturnType<typeof readDb>): Product {
  return {
    ...product,
    category: db.categories.find((c) => c.id === product.categoryId)?.name ?? '',
    brand: product.brandId ? (db.brands.find((b) => b.id === product.brandId)?.name ?? null) : null,
  };
}

function sortProducts(items: DbProduct[], sort: CatalogSort): DbProduct[] {
  const copy = [...items];
  switch (sort) {
    case 'price-asc':
      return copy.sort((a, b) => a.price - b.price);
    case 'price-desc':
      return copy.sort((a, b) => b.price - a.price);
    case 'name-asc':
      return copy.sort((a, b) => a.name.localeCompare(b.name, 'ru'));
    case 'newest':
      return copy.sort((a, b) => b.createdAt.localeCompare(a.createdAt));
    case 'popular':
    default:
      return copy.sort((a, b) => Number(b.isPopular) - Number(a.isPopular) || b.rating - a.rating);
  }
}

export function handleCategories(): MockResponse {
  const db = readDb();
  const counts = new Map<string, number>();
  for (const product of db.products) {
    if (product.isActive) {
      counts.set(product.categoryId, (counts.get(product.categoryId) ?? 0) + 1);
    }
  }
  const result: CategoryWithCount[] = db.categories.map((category) => ({
    ...category,
    count: counts.get(category.id) ?? 0,
  }));
  return result;
}

export function handleBrands(ctx: MockRequestContext): MockResponse {
  const db = readDb();
  const category = ctx.params.get('category');
  const counts = new Map<string, number>();
  for (const product of db.products) {
    if (!product.isActive) {
      continue;
    }
    if (category && product.categoryId !== category) {
      continue;
    }
    if (product.brandId) {
      counts.set(product.brandId, (counts.get(product.brandId) ?? 0) + 1);
    }
  }
  const result: BrandWithCount[] = db.brands
    .map((brand) => ({ ...brand, count: counts.get(brand.id) ?? 0 }))
    .filter((brand) => brand.count > 0);
  return result;
}

export function handleProductList(ctx: MockRequestContext): MockResponse {
  const db = readDb();
  const search = (ctx.params.get('search') ?? '').trim().toLowerCase();
  const category = ctx.params.get('category');
  const brandParams = ctx.params.getAll('brand');
  const priceMin = Number(ctx.params.get('priceMin'));
  const priceMax = Number(ctx.params.get('priceMax'));
  const onlyAvailable = ctx.params.get('available') === 'true';
  const sort = (ctx.params.get('sort') ?? 'popular') as CatalogSort;
  const page = Math.max(1, Number(ctx.params.get('page')) || 1);
  const pageSize = Math.min(48, Math.max(1, Number(ctx.params.get('pageSize')) || CATALOG_PAGE_SIZE));

  let items = db.products.filter((product) => product.isActive);

  if (search) {
    items = items.filter((product) => {
      const brandName = product.brandId ? (db.brands.find((b) => b.id === product.brandId)?.name ?? '') : '';
      return (
        product.name.toLowerCase().includes(search) ||
        product.sku.toLowerCase().includes(search) ||
        brandName.toLowerCase().includes(search)
      );
    });
  }
  if (category) {
    items = items.filter((product) => product.categoryId === category);
  }
  if (brandParams.length > 0) {
    items = items.filter((product) => product.brandId !== null && brandParams.includes(product.brandId));
  }
  if (Number.isFinite(priceMin) && priceMin > 0) {
    items = items.filter((product) => product.price >= priceMin);
  }
  if (Number.isFinite(priceMax) && priceMax > 0) {
    items = items.filter((product) => product.price <= priceMax);
  }
  if (onlyAvailable) {
    items = items.filter((product) => product.stock > 0);
  }

  items = sortProducts(items, sort);
  const total = items.length;
  const start = (page - 1) * pageSize;
  const paged = items.slice(start, start + pageSize);

  const result: Paged<Product> = {
    items: paged.map((product) => toDto(product, db)),
    total,
    page,
    pageSize,
  };
  return result;
}

export function handleProductDetail(match: RegExpMatchArray, _ctx: MockRequestContext): MockResponse {
  const db = readDb();
  const product = db.products.find((p) => p.id === match[1]);
  if (!product || !product.isActive) {
    throw MockHttpError.notFound('Товар не найден');
  }
  return toDto(product, db);
}
