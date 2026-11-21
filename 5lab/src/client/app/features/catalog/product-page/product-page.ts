import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { CatalogService } from '../../../core/services/catalog.service';
import { CartService } from '../../../core/services/cart.service';
import { NotificationService } from '../../../core/services/notification.service';
import { RubPipe } from '../../../shared/pipes/rub.pipe';
import { Product } from '../../../shared/models';

@Component({
  selector: 'app-product-page',
  imports: [RouterLink, RubPipe],
  templateUrl: './product-page.html',
  styleUrl: './product-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductPage {
  readonly id = input.required<string>();

  private readonly catalogApi = inject(CatalogService);
  private readonly cart = inject(CartService);
  private readonly notifications = inject(NotificationService);

  readonly product = signal<Product | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly quantity = signal(1);

  readonly inCartQty = computed(() => this.cart.quantityInCart().get(this.product()?.id ?? '') ?? 0);

  constructor() {
    effect(() => {
      const id = this.id();
      this.loading.set(true);
      this.notFound.set(false);
      this.quantity.set(1);
      this.catalogApi.product(id).subscribe({
        next: (product) => {
          this.product.set(product);
          this.loading.set(false);
        },
        error: () => {
          this.notFound.set(true);
          this.loading.set(false);
        },
      });
    });
  }

  inc(): void {
    const stock = this.product()?.stock ?? 0;
    if (this.quantity() < stock) {
      this.quantity.update((value) => value + 1);
    } else {
      this.notifications.warn(`В наличии только ${stock} шт.`);
    }
  }

  dec(): void {
    if (this.quantity() > 1) {
      this.quantity.update((value) => value - 1);
    }
  }

  addToCart(): void {
    const product = this.product();
    if (!product) {
      return;
    }
    this.cart.add(product.id, this.quantity()).subscribe({
      error: (err) => this.notifications.error(NotificationService.messageOf(err)),
    });
  }
}
