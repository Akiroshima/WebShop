import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ConfirmationService } from 'primeng/api';
import { Select } from 'primeng/select';

import { NotificationService } from '../../../core/services/notification.service';
import { OrdersService } from '../../../core/services/orders.service';
import { Pager } from '../../../shared/components/pager/pager';
import { RuDatePipe } from '../../../shared/pipes/ru-date.pipe';
import { RubPipe } from '../../../shared/pipes/rub.pipe';
import { ORDER_STATUS_LABELS, Order, OrderStatus, Paged } from '../../../shared/models';

const PAGE_SIZE = 10;

interface StatusOption {
  value: OrderStatus | null;
  label: string;
}

const STATUS_TRANSITIONS: Record<OrderStatus, { value: OrderStatus; label: string }[]> = {
  new: [
    { value: 'confirmed', label: 'Подтвердить' },
    { value: 'cancelled', label: 'Отменить' },
  ],
  confirmed: [
    { value: 'delivered', label: 'Доставлен' },
    { value: 'cancelled', label: 'Отменить' },
  ],
  delivered: [],
  cancelled: [],
};

@Component({
  selector: 'app-admin-orders-page',
  imports: [FormsModule, Select, RubPipe, RuDatePipe, Pager],
  providers: [ConfirmationService],
  templateUrl: './orders.html',
  styleUrl: './orders.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminOrdersPage {
  private readonly ordersApi = inject(OrdersService);
  private readonly notifications = inject(NotificationService);
  private readonly confirmations = inject(ConfirmationService);

  readonly result = signal<Paged<Order> | null>(null);
  readonly loading = signal(true);
  readonly statusFilter = signal<OrderStatus | null>(null);
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

  readonly statusOptions: StatusOption[] = [
    { value: null, label: 'Все статусы' },
    { value: 'new', label: ORDER_STATUS_LABELS['new'] },
    { value: 'confirmed', label: ORDER_STATUS_LABELS['confirmed'] },
    { value: 'delivered', label: ORDER_STATUS_LABELS['delivered'] },
    { value: 'cancelled', label: ORDER_STATUS_LABELS['cancelled'] },
  ];

  constructor() {
    this.reload(1);
  }

  reload(page: number = this.page()): void {
    this.loading.set(true);
    this.ordersApi.adminOrders(this.statusFilter(), page, PAGE_SIZE).subscribe({
      next: (paged) => {
        this.result.set(paged);
        this.page.set(paged.page);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.notifications.error(NotificationService.messageOf(err));
      },
    });
  }

  onFilterChange(status: OrderStatus | null): void {
    this.statusFilter.set(status);
    this.reload(1);
  }

  transitions(order: Order): { value: OrderStatus; label: string }[] {
    return STATUS_TRANSITIONS[order.status];
  }

  statusLabel(status: OrderStatus): string {
    return ORDER_STATUS_LABELS[status];
  }

  applyStatus(order: Order, status: OrderStatus): void {
    const run = () => {
      this.ordersApi.adminSetStatus(order.id, status).subscribe({
        next: () => {
          this.notifications.success(`Заказ №${order.number}: ${this.statusLabel(status)}`);
          this.reload();
        },
        error: (err) => this.notifications.error(NotificationService.messageOf(err)),
      });
    };

    if (status === 'cancelled') {
      this.confirmations.confirm({
        header: 'Отменить заказ?',
        message: `Заказ №${order.number} будет отменён, а товары вернутся на склад.`,
        acceptLabel: 'Отменить заказ',
        rejectLabel: 'Оставить',
        acceptButtonStyleClass: 'p-button-danger',
        accept: run,
      });
    } else {
      run();
    }
  }
}
