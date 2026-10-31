import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { OrdersService } from '../../../core/services/orders.service';
import { Pager } from '../../../shared/components/pager/pager';
import { RuDatePipe } from '../../../shared/pipes/ru-date.pipe';
import { RubPipe } from '../../../shared/pipes/rub.pipe';
import { ORDER_STATUS_LABELS, Order, Paged } from '../../../shared/models';

const PAGE_SIZE = 10;

@Component({
  selector: 'app-orders-page',
  imports: [RouterLink, RubPipe, RuDatePipe, Pager],
  templateUrl: './orders-list.html',
  styleUrl: './orders-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrdersPage {
  private readonly ordersApi = inject(OrdersService);

  readonly result = signal<Paged<Order> | null>(null);
  readonly loading = signal(true);
  readonly page = signal(1);

  readonly orders = computed(() => this.result()?.items ?? []);
  readonly totalPages = computed(() => {
    const data = this.result();
    return data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1;
  });
  readonly range = computed(() => {
    const data = this.result();
    if (!data || data.total === 0) {
      return '';
    }
    const start = (data.page - 1) * data.pageSize + 1;
    const end = Math.min(data.total, data.page * data.pageSize);
    return `Показано ${start}–${end} из ${data.total}`;
  });

  constructor() {
    this.reload(this.page());
  }

  reload(page: number): void {
    this.loading.set(true);
    this.ordersApi.myOrders(page, PAGE_SIZE).subscribe({
      next: (paged) => {
        this.result.set(paged);
        this.page.set(paged.page);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  setPage(page: number): void {
    this.reload(page);
  }

  statusLabel(status: Order['status']): string {
    return ORDER_STATUS_LABELS[status];
  }
}
