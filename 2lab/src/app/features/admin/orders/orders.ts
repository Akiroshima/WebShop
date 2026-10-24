import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ConfirmationService } from 'primeng/api';
import { Select } from 'primeng/select';

import { NotificationService } from '../../../core/services/notification.service';
import { OrdersService } from '../../../core/services/orders.service';
import { RuDatePipe } from '../../../shared/pipes/ru-date.pipe';
import { RubPipe } from '../../../shared/pipes/rub.pipe';
import { ORDER_STATUS_LABELS, Order, OrderStatus } from '../../../shared/models';

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
  imports: [FormsModule, Select, RubPipe, RuDatePipe],
  providers: [ConfirmationService],
  templateUrl: './orders.html',
  styleUrl: './orders.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminOrdersPage {
  private readonly ordersApi = inject(OrdersService);
  private readonly notifications = inject(NotificationService);
  private readonly confirmations = inject(ConfirmationService);

  readonly orders = signal<Order[]>([]);
  readonly loading = signal(true);
  readonly statusFilter = signal<OrderStatus | null>(null);

  readonly statusOptions: StatusOption[] = [
    { value: null, label: 'Все статусы' },
    { value: 'new', label: ORDER_STATUS_LABELS['new'] },
    { value: 'confirmed', label: ORDER_STATUS_LABELS['confirmed'] },
    { value: 'delivered', label: ORDER_STATUS_LABELS['delivered'] },
    { value: 'cancelled', label: ORDER_STATUS_LABELS['cancelled'] },
  ];

  constructor() {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.ordersApi.adminOrders(this.statusFilter()).subscribe({
      next: (orders) => {
        this.orders.set(orders);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.notifications.error(NotificationService.messageOf(err));
      },
    });
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
