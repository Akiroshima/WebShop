import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { CheckoutPayload, CheckoutResult, Order, OrderStatus } from '../../shared/models';

import { API_BASE_URL } from '../config/api';

@Injectable({ providedIn: 'root' })
export class OrdersService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_BASE_URL;

  checkout(payload: CheckoutPayload): Observable<CheckoutResult> {
    return this.http.post<CheckoutResult>(`${this.apiUrl}/orders`, payload);
  }

  myOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.apiUrl}/orders`);
  }

  order(id: string): Observable<Order> {
    return this.http.get<Order>(`${this.apiUrl}/orders/${id}`);
  }

  adminOrders(status?: OrderStatus | null): Observable<Order[]> {
    const params = new HttpParams({ fromObject: status ? { status } : {} });
    return this.http.get<Order[]>(`${this.apiUrl}/admin/orders`, { params });
  }

  adminSetStatus(orderId: string, status: OrderStatus): Observable<Order> {
    return this.http.put<Order>(`${this.apiUrl}/admin/orders/${orderId}/status`, { status });
  }
}
