import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { CartMutationResult, CartState } from '../../shared/models';

import { API_BASE_URL } from '../config/api';
import { NotificationService } from './notification.service';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly notifications = inject(NotificationService);
  private readonly apiUrl = `${API_BASE_URL}/cart`;

  /** Текущее состояние корзины; null — ещё не загружена. */
  readonly cart = signal<CartState | null>(null);

  /** Количество товаров для бейджа в шапке. */
  readonly badgeCount = computed(() => this.cart()?.itemsCount ?? 0);

  /** Карта productId → количество (для карточек каталога «В корзине − N +»). */
  readonly quantityInCart = computed(() => {
    const map = new Map<string, number>();
    for (const item of this.cart()?.items ?? []) {
      map.set(item.productId, item.quantity);
    }
    return map;
  });

  load(): Observable<CartState> {
    return this.http.get<CartState>(this.apiUrl).pipe(tap((cart) => this.cart.set(cart)));
  }

  add(productId: string, quantity = 1): Observable<CartMutationResult> {
    return this.http.post<CartMutationResult>(`${this.apiUrl}/items`, { productId, quantity }).pipe(
      tap((result) => {
        this.cart.set(result.cart);
        if (result.notice) {
          this.notifications.warn(result.notice);
        } else {
          this.notifications.success('Товар добавлен в корзину');
        }
      }),
    );
  }

  setQuantity(productId: string, quantity: number): Observable<CartMutationResult> {
    return this.http
      .put<CartMutationResult>(`${this.apiUrl}/items/${productId}`, { quantity })
      .pipe(tap((result) => this.cart.set(result.cart)));
  }

  remove(productId: string): Observable<CartMutationResult> {
    return this.http
      .delete<CartMutationResult>(`${this.apiUrl}/items/${productId}`)
      .pipe(tap((result) => this.cart.set(result.cart)));
  }

  applyPromo(code: string): Observable<CartMutationResult> {
    return this.http.post<CartMutationResult>(`${this.apiUrl}/promo`, { code }).pipe(
      tap((result) => {
        this.cart.set(result.cart);
        this.notifications.success(`Промокод ${result.cart.promo?.code ?? ''} применён`);
      }),
    );
  }

  removePromo(): Observable<CartMutationResult> {
    return this.http.delete<CartMutationResult>(`${this.apiUrl}/promo`).pipe(
      tap((result) => {
        this.cart.set(result.cart);
        this.notifications.info('Промокод убран');
      }),
    );
  }
}
