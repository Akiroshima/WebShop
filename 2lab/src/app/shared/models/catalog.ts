export type CatalogSort = 'popular' | 'newest' | 'price-asc' | 'price-desc' | 'name-asc';

export interface CatalogSortOption {
  value: CatalogSort;
  label: string;
}

export const CATALOG_SORT_OPTIONS: CatalogSortOption[] = [
  { value: 'popular', label: 'Сначала популярные' },
  { value: 'newest', label: 'Сначала новые' },
  { value: 'price-asc', label: 'Цена: по возрастанию' },
  { value: 'price-desc', label: 'Цена: по убыванию' },
  { value: 'name-asc', label: 'Название: А–Я' },
];

export interface Category {
  id: string;
  name: string;
}

export interface CategoryWithCount extends Category {
  count: number;
}

export interface Brand {
  id: string;
  name: string;
}

export interface BrandWithCount extends Brand {
  count: number;
}

export interface Product {
  id: string;
  sku: string;
  name: string;
  description: string;
  categoryId: string;
  category: string;
  brandId: string | null;
  brand: string | null;
  price: number;
  stock: number;
  imageUrl: string;
  rating: number;
  isPopular: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface Paged<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}
