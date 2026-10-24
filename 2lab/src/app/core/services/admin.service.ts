import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { PublicUser } from '../../shared/models';
import { Product } from '../../shared/models/catalog';

import { API_BASE_URL } from '../config/api';

export interface ProductDraft {
  sku: string;
  name: string;
  description: string;
  categoryId: string;
  brandId: string | null;
  price: number;
  stock: number;
  imageUrl: string;
  isActive: boolean;
}

export interface AdminUser extends PublicUser {
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${API_BASE_URL}/admin`;

  products(search?: string): Observable<Product[]> {
    const params = new HttpParams({ fromObject: search ? { search } : {} });
    return this.http.get<Product[]>(`${this.apiUrl}/products`, { params });
  }

  createProduct(draft: ProductDraft): Observable<Product> {
    return this.http.post<Product>(`${this.apiUrl}/products`, draft);
  }

  updateProduct(id: string, draft: ProductDraft): Observable<Product> {
    return this.http.put<Product>(`${this.apiUrl}/products/${id}`, draft);
  }

  deleteProduct(id: string): Observable<unknown> {
    return this.http.delete(`${this.apiUrl}/products/${id}`);
  }

  users(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${this.apiUrl}/users`);
  }
}
