import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { OrdersService } from '../../../core/services/orders.service';
import { RuDatePipe } from '../../../shared/pipes/ru-date.pipe';
import { RubPipe } from '../../../shared/pipes/rub.pipe';
import { ORDER_STATUS_LABELS, Order } from '../../../shared/models';

@Component({
  selector: 'app-order-detail-page',
  imports: [RouterLink, RubPipe, RuDatePipe],
  templateUrl: './order-detail.html',
  styleUrl: './order-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderDetailPage {
  readonly id = input.required<string>();

  private readonly ordersApi = inject(OrdersService);

  readonly order = signal<Order | null>(null);
  readonly loading = signal(true);

  readonly statusLabel = computed(() => {
    const order = this.order();
    return order ? ORDER_STATUS_LABELS[order.status] : '';
  });

  readonly addressLine = computed(() => {
    const order = this.order();
    if (!order) {
      return '';
    }
    const parts = [
      `${order.address.city}${order.address.postalCode ? ', ' + order.address.postalCode : ''}`,
      `${order.address.street}, д. ${order.address.house}`,
      order.address.apartment ? `кв. ${order.address.apartment}` : null,
      order.address.entrance ? `подъезд ${order.address.entrance}` : null,
      order.address.floor ? `этаж ${order.address.floor}` : null,
    ].filter(Boolean);
    return parts.join(', ');
  });

  constructor() {
    effect(() => {
      const id = this.id();
      this.ordersApi.order(id).subscribe({
        next: (order) => {
          this.order.set(order);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
    });
  }
}
