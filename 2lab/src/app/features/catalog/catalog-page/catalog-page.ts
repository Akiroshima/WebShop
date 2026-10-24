import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Params, Router, RouterLink } from '@angular/router';

import { NgTemplateOutlet } from '@angular/common';

import { Checkbox } from 'primeng/checkbox';
import { Drawer } from 'primeng/drawer';
import { Select } from 'primeng/select';

import { CatalogService } from '../../../core/services/catalog.service';
import { CartService } from '../../../core/services/cart.service';
import { NotificationService } from '../../../core/services/notification.service';
import { RubPipe } from '../../../shared/pipes/rub.pipe';
import { BrandWithCount, CATALOG_SORT_OPTIONS, CategoryWithCount, CatalogSort, Paged, Product } from '../../../shared/models';

@Component({
  selector: 'app-catalog-page',
  imports: [FormsModule, RouterLink, NgTemplateOutlet, Checkbox, Select, Drawer, RubPipe],
  templateUrl: './catalog-page.html',
  styleUrl: './catalog-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CatalogPage {
  private readonly catalogApi = inject(CatalogService);
  private readonly cart = inject(CartService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly notifications = inject(NotificationService);

  readonly sortOptions = CATALOG_SORT_OPTIONS;
  readonly filtersOpen = signal(false);

  readonly categories = signal<CategoryWithCount[]>([]);
  readonly brands = signal<BrandWithCount[]>([]);
  readonly result = signal<Paged<Product> | null>(null);
  readonly loading = signal(true);

  readonly brandSearch = signal('');
  readonly showAllBrands = signal(false);

  /** Текущие query-параметры — обновляются подпиской в конструкторе (deep-link и шаринг работают). */
  private readonly queryParams = signal<ParamMap>(this.route.snapshot.queryParamMap);

  // ----- Состояние фильтров — всегда из URL (deep-link и шаринг работают) -----
  readonly search = computed(() => this.queryParams().get('search') ?? '');
  readonly category = computed(() => this.queryParams().get('category') ?? '');
  readonly selectedBrands = computed(() => this.queryParams().getAll('brand') ?? []);
  readonly priceMin = computed(() => this.queryParams().get('priceMin') ?? '');
  readonly priceMax = computed(() => this.queryParams().get('priceMax') ?? '');
  readonly onlyAvailable = computed(() => this.queryParams().get('available') === 'true');
  readonly sort = computed<CatalogSort>(() => (this.queryParams().get('sort') as CatalogSort) || 'popular');
  readonly page = computed(() => Math.max(1, Number(this.queryParams().get('page')) || 1));

  readonly priceFrom = signal('');
  readonly priceTo = signal('');

  readonly visibleBrands = computed(() => {
    const query = this.brandSearch().trim().toLowerCase();
    const list = query
      ? this.brands().filter((brand) => brand.name.toLowerCase().includes(query))
      : this.brands();
    return this.showAllBrands() ? list : list.slice(0, 6);
  });

  readonly activeCategory = computed(() => this.categories().find((c) => c.id === this.category()) ?? null);

  readonly totalPages = computed(() => {
    const data = this.result();
    return data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1;
  });

  readonly pages = computed(() => {
    const total = this.totalPages();
    const current = this.page();
    const window = 7;
    let start = Math.max(1, current - Math.floor(window / 2));
    const end = Math.min(total, start + window - 1);
    start = Math.max(1, end - window + 1);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  });

  readonly shownRange = computed(() => {
    const data = this.result();
    if (!data || data.total === 0) {
      return '';
    }
    const start = (data.page - 1) * data.pageSize + 1;
    const end = Math.min(data.total, data.page * data.pageSize);
    return `Показано ${start}–${end} из ${data.total}`;
  });

  readonly hasActiveFilters = computed(
    () =>
      this.search() !== '' ||
      this.category() !== '' ||
      this.selectedBrands().length > 0 ||
      this.priceMin() !== '' ||
      this.priceMax() !== '' ||
      this.onlyAvailable(),
  );

  constructor() {
    this.catalogApi.categories().subscribe({ next: (categories) => this.categories.set(categories) });

    // Смена query-параметров (фильтры, сортировка, страница) перезагружает список и бренды.
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      this.queryParams.set(params);
      this.priceFrom.set(params.get('priceMin') ?? '');
      this.priceTo.set(params.get('priceMax') ?? '');
      this.loadBrands(params.get('category'));
      this.loadProducts(params);
    });
  }

  private loadBrands(categoryId: string | null): void {
    this.catalogApi.brands(categoryId).subscribe({ next: (brands) => this.brands.set(brands) });
  }

  private loadProducts(params: ParamMap): void {
    this.loading.set(true);
    this.catalogApi
      .products({
        search: params.get('search') ?? undefined,
        category: params.get('category') || null,
        brands: params.getAll('brand'),
        priceMin: Number(params.get('priceMin')) > 0 ? Number(params.get('priceMin')) : null,
        priceMax: Number(params.get('priceMax')) > 0 ? Number(params.get('priceMax')) : null,
        onlyAvailable: params.get('available') === 'true',
        sort: (params.get('sort') as CatalogSort) || 'popular',
        page: Math.max(1, Number(params.get('page')) || 1),
      })
      .subscribe({
        next: (paged) => {
          this.result.set(paged);
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          this.notifications.error(NotificationService.messageOf(err));
        },
      });
  }

  // ----- Навигация: контролы меняют query-параметры, состояние читается из URL -----
  private go(patch: Params): void {
    const params = this.queryParams();
    const next: Params = {
      search: params.get('search') ?? null,
      category: params.get('category') ?? null,
      brand: params.getAll('brand') ?? [],
      priceMin: params.get('priceMin') ?? null,
      priceMax: params.get('priceMax') ?? null,
      available: params.get('available') ?? null,
      sort: params.get('sort') ?? null,
      page: params.get('page') ?? null,
      ...patch,
    };
    void this.router.navigate([], { relativeTo: this.route, queryParams: next });
  }

  setCategory(id: string | null): void {
    this.go({ category: id, page: null });
  }

  toggleBrand(id: string, checked: boolean): void {
    const brands = new Set(this.selectedBrands());
    if (checked) {
      brands.add(id);
    } else {
      brands.delete(id);
    }
    this.go({ brand: [...brands], page: null });
  }

  applyPrice(): void {
    const min = Number(this.priceFrom()) > 0 ? String(Math.floor(Number(this.priceFrom()))) : null;
    const max = Number(this.priceTo()) > 0 ? String(Math.floor(Number(this.priceTo()))) : null;
    if (min && max && Number(min) > Number(max)) {
      this.notifications.warn('Минимальная цена не может превышать максимальную');
      return;
    }
    this.go({ priceMin: min, priceMax: max, page: null });
  }

  setOnlyAvailable(checked: boolean): void {
    this.go({ available: checked ? 'true' : null, page: null });
  }

  setSort(sort: CatalogSort): void {
    this.go({ sort: sort === 'popular' ? null : sort, page: null });
  }

  setPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }
    this.go({ page: page === 1 ? null : String(page) });
  }

  resetFilters(): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  removeChip(kind: 'search' | 'category' | 'brand' | 'price' | 'available', brandId?: string): void {
    switch (kind) {
      case 'search':
        this.go({ search: null, page: null });
        break;
      case 'category':
        this.setCategory(null);
        break;
      case 'brand':
        this.toggleBrand(brandId ?? '', false);
        break;
      case 'price':
        this.go({ priceMin: null, priceMax: null, page: null });
        break;
      case 'available':
        this.setOnlyAvailable(false);
        break;
    }
  }

  // ----- Корзина в карточках -----
  qtyOf(productId: string): number {
    return this.cart.quantityInCart().get(productId) ?? 0;
  }

  addToCart(product: Product): void {
    this.cart.add(product.id).subscribe({
      error: (err) => this.notifications.error(NotificationService.messageOf(err)),
    });
  }

  changeQty(product: Product, delta: number): void {
    const current = this.qtyOf(product.id);
    const next = current + delta;
    if (next <= 0) {
      this.cart.remove(product.id).subscribe();
      return;
    }
    if (next > product.stock) {
      this.notifications.warn(`В наличии только ${product.stock} шт.`);
      return;
    }
    this.cart.setQuantity(product.id, next).subscribe({
      error: (err) => this.notifications.error(NotificationService.messageOf(err)),
    });
  }

  brandOf(brandId: string): BrandWithCount | null {
    return this.brands().find((brand) => brand.id === brandId) ?? null;
  }

  trackProduct = (_: number, product: Product) => product.id;
}
