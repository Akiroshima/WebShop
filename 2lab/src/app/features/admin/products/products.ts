import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ConfirmationService } from 'primeng/api';
import { Checkbox } from 'primeng/checkbox';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { Select } from 'primeng/select';

import { AdminService, ProductDraft } from '../../../core/services/admin.service';
import { CatalogService } from '../../../core/services/catalog.service';
import { NotificationService } from '../../../core/services/notification.service';
import { RubPipe } from '../../../shared/pipes/rub.pipe';
import { Brand, Category, Product } from '../../../shared/models';

@Component({
  selector: 'app-admin-products-page',
  imports: [FormsModule, Checkbox, Dialog, InputNumber, Select, RubPipe],
  providers: [ConfirmationService],
  templateUrl: './products.html',
  styleUrl: './products.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminProductsPage {
  private readonly adminApi = inject(AdminService);
  private readonly catalogApi = inject(CatalogService);
  private readonly notifications = inject(NotificationService);
  private readonly confirmations = inject(ConfirmationService);

  readonly products = signal<Product[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly brands = signal<Brand[]>([]);
  readonly loading = signal(true);

  readonly search = signal('');
  readonly dialogVisible = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<string | null>(null);

  readonly draft = signal<ProductDraft>({
    sku: '',
    name: '',
    description: '',
    categoryId: '',
    brandId: null,
    price: 0,
    stock: 0,
    imageUrl: 'assets/img/products/korm.svg',
    isActive: true,
  });

  constructor() {
    this.reload();
    this.catalogApi.categories().subscribe((categories) => this.categories.set(categories));
    this.catalogApi.brands(null).subscribe((brands) => this.brands.set(brands));
  }

  reload(): void {
    this.adminApi.products(this.search() || undefined).subscribe({
      next: (products) => {
        this.products.set(products);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.notifications.error(NotificationService.messageOf(err));
      },
    });
  }

  openCreate(): void {
    this.editingId.set(null);
    this.draft.set({
      sku: '',
      name: '',
      description: '',
      categoryId: this.categories()[0]?.id ?? '',
      brandId: null,
      price: 0,
      stock: 0,
      imageUrl: 'assets/img/products/korm.svg',
      isActive: true,
    });
    this.dialogVisible.set(true);
  }

  openEdit(product: Product): void {
    this.editingId.set(product.id);
    this.draft.set({
      sku: product.sku,
      name: product.name,
      description: product.description,
      categoryId: product.categoryId,
      brandId: product.brandId,
      price: product.price,
      stock: product.stock,
      imageUrl: product.imageUrl,
      isActive: product.isActive,
    });
    this.dialogVisible.set(true);
  }

  update<K extends keyof ProductDraft>(key: K, value: ProductDraft[K]): void {
    this.draft.update((draft) => ({ ...draft, [key]: value }));
  }

  save(): void {
    const draft = this.draft();
    if (!draft.sku.trim() || !draft.name.trim() || !draft.categoryId || draft.price < 0 || draft.stock < 0) {
      this.notifications.warn('Заполните артикул, название, категорию; цена и остаток — не отрицательные');
      return;
    }
    this.saving.set(true);
    const request$ = this.editingId()
      ? this.adminApi.updateProduct(this.editingId()!, draft)
      : this.adminApi.createProduct(draft);
    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notifications.success(this.editingId() ? 'Товар обновлён' : 'Товар добавлен в каталог');
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.notifications.error(NotificationService.messageOf(err));
      },
    });
  }

  askDelete(product: Product): void {
    this.confirmations.confirm({
      header: 'Удалить товар?',
      message: `«${product.name}» исчезнет из каталога и всех корзин покупателей.`,
      acceptLabel: 'Удалить',
      rejectLabel: 'Отмена',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.adminApi.deleteProduct(product.id).subscribe({
          next: () => {
            this.notifications.info('Товар удалён');
            this.reload();
          },
          error: (err) => this.notifications.error(NotificationService.messageOf(err)),
        });
      },
    });
  }
}
