import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { CheckoutPayload, CheckoutResult, Order, OrderStatus, Paged } from '../../shared/models';

import { API_BASE_URL } from '../config/api';

@Injectable({ providedIn: 'root' })
export class OrdersService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_BASE_URL;

  checkout(payload: CheckoutPayload): Observable<CheckoutResult> {
    return this.http.post<CheckoutResult>(`${this.apiUrl}/orders`, payload);
  }

  /** Мои заказы — постранично ({items, total, page, pageSize}). */
  myOrders(page = 1, pageSize = 10): Observable<Paged<Order>> {
    const params = new HttpParams({ fromObject: { page: String(page), pageSize: String(pageSize) } });
    return this.http.get<Paged<Order>>(`${this.apiUrl}/orders`, { params });
  }

  order(id: string): Observable<Order> {
    return this.http.get<Order>(`${this.apiUrl}/orders/${id}`);
  }

  /** Заказы магазина с фильтром по статусу — постранично. */
  adminOrders(status?: OrderStatus | null, page = 1, pageSize = 10): Observable<Paged<Order>> {
    const params = new HttpParams({
      fromObject: {
        ...(status ? { status } : {}),
        page: String(page),
        pageSize: String(pageSize),
      },
    });
    return this.http.get<Paged<Order>>(`${this.apiUrl}/admin/orders`, { params });
  }

  adminSetStatus(orderId: string, status: OrderStatus): Observable<Order> {
    return this.http.put<Order>(`${this.apiUrl}/admin/orders/${orderId}/status`, { status });
  }
}
