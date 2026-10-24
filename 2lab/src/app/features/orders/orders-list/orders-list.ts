import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { OrdersService } from '../../../core/services/orders.service';
import { RuDatePipe } from '../../../shared/pipes/ru-date.pipe';
import { RubPipe } from '../../../shared/pipes/rub.pipe';
import { ORDER_STATUS_LABELS, Order } from '../../../shared/models';

@Component({
  selector: 'app-orders-page',
  imports: [RouterLink, RubPipe, RuDatePipe],
  templateUrl: './orders-list.html',
  styleUrl: './orders-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrdersPage {
  private readonly ordersApi = inject(OrdersService);

  readonly orders = signal<Order[]>([]);
  readonly loading = signal(true);

  constructor() {
    this.ordersApi.myOrders().subscribe({
      next: (orders) => {
        this.orders.set(orders);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  statusLabel(status: Order['status']): string {
    return ORDER_STATUS_LABELS[status];
  }
}
