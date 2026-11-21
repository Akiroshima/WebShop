import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { MessageService } from 'primeng/api';

import { AdminService } from './admin.service';
import { CartService } from './cart.service';
import { CatalogService } from './catalog.service';
import { OrdersService } from './orders.service';

/** Сервисы ходят в реальный REST API /api/v1: проверяем адреса, параметры и разбор ответов. */
describe('API-сервисы', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), MessageService],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('CatalogService.products передаёт фильтры, сортировку и страницу', () => {
    TestBed.inject(CatalogService)
      .products({
        search: 'корм',
        category: 'c-food',
        brands: ['b-triol', 'b-monge'],
        priceMin: 100,
        priceMax: 2000,
        onlyAvailable: true,
        sort: 'price-asc',
        page: 2,
        pageSize: 12,
      })
      .subscribe();

    const req = http.expectOne(
      (request) =>
        request.url === '/api/v1/catalog/products' &&
        request.params.get('search') === 'корм' &&
        request.params.get('category') === 'c-food' &&
        request.params.getAll('brand')?.includes('b-triol') === true &&
        request.params.getAll('brand')?.includes('b-monge') === true &&
        request.params.get('priceMin') === '100' &&
        request.params.get('priceMax') === '2000' &&
        request.params.get('available') === 'true' &&
        request.params.get('sort') === 'price-asc' &&
        request.params.get('page') === '2',
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 2, pageSize: 12 });
  });

  it('CartService.add отправляет productId и quantity, разбирает результат', () => {
    const cart = { items: [], itemsCount: 0, subtotal: 0, promo: null, discount: 0, total: 0 };
    TestBed.inject(CartService)
      .add('p-1', 2)
      .subscribe((result) => expect(result.cart.itemsCount).toBe(0));

    const req = http.expectOne('/api/v1/cart/items');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ productId: 'p-1', quantity: 2 });
    req.flush({ cart });
  });

  it('OrdersService.myOrders запрашивает страницу с page/pageSize', () => {
    TestBed.inject(OrdersService).myOrders(3, 10).subscribe();

    const req = http.expectOne(
      (request) =>
        request.url === '/api/v1/orders' &&
        request.params.get('page') === '3' &&
        request.params.get('pageSize') === '10',
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 3, pageSize: 10 });
  });

  it('OrdersService.adminOrders передаёт фильтр статуса', () => {
    TestBed.inject(OrdersService).adminOrders('new', 1, 10).subscribe();

    const req = http.expectOne(
      (request) =>
        request.url === '/api/v1/admin/orders' && request.params.get('status') === 'new',
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 1, pageSize: 10 });
  });

  it('AdminService.products передаёт поиск и страницу', () => {
    TestBed.inject(AdminService).products('rc', 2, 10).subscribe();

    const req = http.expectOne(
      (request) =>
        request.url === '/api/v1/admin/products' &&
        request.params.get('search') === 'rc' &&
        request.params.get('page') === '2',
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 2, pageSize: 10 });
  });

  it('AdminService.createProduct отправляет черновик POST-ом', () => {
    const draft = {
      sku: 'RC-900',
      name: 'Тест',
      description: '',
      categoryId: 'c-food',
      brandId: null,
      price: 100,
      stock: 1,
      imageUrl: 'assets/img/products/korm.svg',
      isActive: true,
    };
    TestBed.inject(AdminService).createProduct(draft).subscribe();

    const req = http.expectOne('/api/v1/admin/products');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(draft);
    req.flush({ id: 'p-new', ...draft });
  });
});
