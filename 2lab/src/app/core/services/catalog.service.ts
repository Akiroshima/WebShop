import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { BrandWithCount, CategoryWithCount, Paged, Product, CatalogSort } from '../../shared/models';

import { API_BASE_URL } from '../config/api';

export interface CatalogQuery {
  search?: string;
  category?: string | null;
  brands?: string[];
  priceMin?: number | null;
  priceMax?: number | null;
  onlyAvailable?: boolean;
  sort?: CatalogSort;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${API_BASE_URL}/catalog`;

  categories(): Observable<CategoryWithCount[]> {
    return this.http.get<CategoryWithCount[]>(`${this.apiUrl}/categories`);
  }

  brands(categoryId?: string | null): Observable<BrandWithCount[]> {
    const params = new HttpParams({ fromObject: categoryId ? { category: categoryId } : {} });
    return this.http.get<BrandWithCount[]>(`${this.apiUrl}/brands`, { params });
  }

  products(query: CatalogQuery): Observable<Paged<Product>> {
    let params = new HttpParams();
    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.category) {
      params = params.set('category', query.category);
    }
    for (const brandId of query.brands ?? []) {
      params = params.append('brand', brandId);
    }
    if (query.priceMin != null) {
      params = params.set('priceMin', String(query.priceMin));
    }
    if (query.priceMax != null) {
      params = params.set('priceMax', String(query.priceMax));
    }
    if (query.onlyAvailable) {
      params = params.set('available', 'true');
    }
    if (query.sort) {
      params = params.set('sort', query.sort);
    }
    if (query.page) {
      params = params.set('page', String(query.page));
    }
    if (query.pageSize) {
      params = params.set('pageSize', String(query.pageSize));
    }
    return this.http.get<Paged<Product>>(`${this.apiUrl}/products`, { params });
  }

  product(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/products/${id}`);
  }
}
