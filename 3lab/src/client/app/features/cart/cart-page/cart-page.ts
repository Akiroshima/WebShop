import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { CartService } from '../../../core/services/cart.service';
import { NotificationService } from '../../../core/services/notification.service';
import { RubPipe } from '../../../shared/pipes/rub.pipe';
import { CartItem } from '../../../shared/models';

@Component({
  selector: 'app-cart-page',
  imports: [RouterLink, RubPipe],
  templateUrl: './cart-page.html',
  styleUrl: './cart-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CartPage {
  readonly cart = inject(CartService);
  private readonly notifications = inject(NotificationService);

  readonly busy = signal<string | null>(null);

  changeQty(item: CartItem, delta: number): void {
    const next = item.quantity + delta;
    if (next <= 0) {
      this.remove(item);
      return;
    }
    if (next > item.stock) {
      this.notifications.warn(`В наличии только ${item.stock} шт.`);
      return;
    }
    this.busy.set(item.productId);
    this.cart.setQuantity(item.productId, next).subscribe({
      error: (err) => this.notifications.error(NotificationService.messageOf(err)),
      complete: () => this.busy.set(null),
    });
  }

  remove(item: CartItem): void {
    this.busy.set(item.productId);
    this.cart.remove(item.productId).subscribe({
      next: () => this.notifications.info('Товар убран из корзины'),
      error: (err) => this.notifications.error(NotificationService.messageOf(err)),
      complete: () => this.busy.set(null),
    });
  }
}
