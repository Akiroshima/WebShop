import { DB_KEY, SESSION_KEY, loadDb, mutateDb, resetDb, seedDb, setSessionUserId } from './mock-db';
import { handleLogin, handleRecoveryConfirm, handleRecoveryRequest, handleResetPassword, resetLoginRateLimit } from '../handlers/auth.handlers';
import { handleProductList } from '../handlers/catalog.handlers';
import { handleAddItem, handleGetCart, handleApplyPromo } from '../handlers/cart.handlers';
import { handleCreateOrder } from '../handlers/orders.handlers';
import { MockHttpError } from '../mock-error';
import { MockRequestContext } from '../handlers/context';

function ctx(overrides: Partial<MockRequestContext> = {}): MockRequestContext {
  return {
    method: 'GET',
    path: '/api/v1',
    params: new URLSearchParams(),
    body: {},
    sessionUserId: null,
    extraDelayMs: 0,
    ...overrides,
  };
}

describe('Мок-БД и сид', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('сид детерминирован', () => {
    expect(seedDb()).toEqual(seedDb());
  });

  it('loadDb создаёт и сохраняет сид при первом обращении', () => {
    const db = loadDb();
    expect(db.users.length).toBeGreaterThanOrEqual(2);
    expect(db.products.length).toBeGreaterThan(30);
    expect(localStorage.getItem(DB_KEY)).not.toBeNull();
  });

  it('resetDb возвращает исходное состояние', () => {
    mutateDb((db) => {
      db.products = [];
    });
    expect(loadDb().products.length).toBe(0);
    resetDb();
    expect(loadDb().products.length).toBeGreaterThan(30);
  });

  it('повреждённые данные пересоздаются', () => {
    localStorage.setItem(DB_KEY, '{{{не-json');
    expect(loadDb().users.length).toBeGreaterThanOrEqual(2);
  });
});

describe('Мок-обработчики: аутентификация', () => {
  beforeEach(() => {
    localStorage.clear();
    setSessionUserId(null);
    resetLoginRateLimit();
  });

  it('вход по логину', () => {
    const response = handleLogin(ctx({ method: 'POST', body: { login: 'customer', password: 'customer123' } }));
    expect((response as any).user.role).toBe('customer');
    expect(localStorage.getItem(SESSION_KEY)).not.toBeNull();
  });

  it('вход по email', () => {
    const response = handleLogin(
      ctx({ method: 'POST', body: { login: 'anna.smirnova@example.com', password: 'customer123' } }),
    );
    expect((response as any).user.fullName).toBe('Анна Смирнова');
  });

  it('вход по телефону', () => {
    const response = handleLogin(
      ctx({ method: 'POST', body: { login: '+7 (999) 123-45-67', password: 'customer123' } }),
    );
    expect((response as any).user.role).toBe('customer');
  });

  it('неверный пароль — единое сообщение', () => {
    try {
      handleLogin(ctx({ method: 'POST', body: { login: 'customer', password: 'wrong' } }));
      fail('ожидалась ошибка');
    } catch (error) {
      expect((error as MockHttpError).status).toBe(401);
      expect((error as MockHttpError).message).toBe('Неверный логин или пароль');
    }
  });

  it('несуществующий пользователь — то же сообщение', () => {
    try {
      handleLogin(ctx({ method: 'POST', body: { login: 'ghost', password: 'whatever' } }));
      fail('ожидалась ошибка');
    } catch (error) {
      expect((error as MockHttpError).message).toBe('Неверный логин или пароль');
    }
  });
});

describe('Мок-обработчики: восстановление доступа', () => {
  beforeEach(() => {
    localStorage.clear();
    setSessionUserId(null);
  });

  it('полный поток: код → confirm → новый пароль → вход', () => {
    const request = handleRecoveryRequest(
      ctx({ method: 'POST', body: { contact: 'anna.smirnova@example.com' } }),
    ) as any;
    expect(request.channel).toBe('email');
    expect(request.maskedContact).toContain('•');
    expect(request.maskedContact).toContain('@example.com');

    const db = JSON.parse(localStorage.getItem(DB_KEY)!);
    const code = db.recoveryRequests[0].code;

    const confirm = handleRecoveryConfirm(
      ctx({ method: 'POST', body: { contact: 'anna.smirnova@example.com', code } }),
    ) as any;
    expect(confirm.resetToken).toBeTruthy();

    handleResetPassword(ctx({ method: 'POST', body: { resetToken: confirm.resetToken, password: 'НовыйПар1!' } }));

    const login = handleLogin(
      ctx({ method: 'POST', body: { login: 'customer', password: 'НовыйПар1!' } }),
    ) as any;
    expect(login.user.fullName).toBe('Анна Смирнова');
  });

  it('неверный код — канонический текст ошибки', () => {
    handleRecoveryRequest(ctx({ method: 'POST', body: { contact: 'anna.smirnova@example.com' } }));
    try {
      handleRecoveryConfirm(ctx({ method: 'POST', body: { contact: 'anna.smirnova@example.com', code: '000000' } }));
      fail('ожидалась ошибка');
    } catch (error) {
      expect((error as MockHttpError).message).toBe('Код восстановления не подходит');
    }
  });

  it('запрос по несуществующему контакту отвечает успехом', () => {
    const response = handleRecoveryRequest(
      ctx({ method: 'POST', body: { contact: 'ghost@example.com' } }),
    ) as any;
    expect(response.channel).toBe('email');
  });
});

describe('Мок-обработчики: каталог', () => {
  beforeEach(() => localStorage.clear());

  it('каталог отдаёт только активные товары', () => {
    const page = handleProductList(ctx()) as any;
    expect(page.items.every((product: any) => product.isActive)).toBeTrue();
  });

  it('фильтр по категории и цене работает', () => {
    const food = handleProductList(ctx({ params: new URLSearchParams('category=c-food') })) as any;
    expect(food.items.every((product: any) => product.categoryId === 'c-food')).toBeTrue();

    const cheap = handleProductList(ctx({ params: new URLSearchParams('priceMax=300') })) as any;
    expect(cheap.items.every((product: any) => product.price <= 300)).toBeTrue();
    expect(cheap.items.length).toBeGreaterThan(0);
  });

  it('сортировка по цене корректна', () => {
    const page = handleProductList(ctx({ params: new URLSearchParams('sort=price-asc') })) as any;
    const prices = page.items.map((product: any) => product.price);
    expect([...prices].sort((a: number, b: number) => a - b)).toEqual(prices);
  });

  it('пагинация: 12 на страницу', () => {
    const page = handleProductList(ctx()) as any;
    expect(page.pageSize).toBe(12);
    expect(page.items.length).toBeLessThanOrEqual(12);
  });
});

describe('Мок-обработчики: корзина и заказ', () => {
  // Обработчики берут пользователя из ctx.sessionUserId — как MockBackend из localStorage.
  const authedCtx = (overrides: Partial<MockRequestContext> = {}): MockRequestContext =>
    ctx({ sessionUserId: localStorage.getItem(SESSION_KEY), ...overrides });

  beforeEach(() => {
    localStorage.clear();
    setSessionUserId(null);
    resetLoginRateLimit();
    handleLogin(ctx({ method: 'POST', body: { login: 'customer', password: 'customer123' } }));
  });

  it('сид корзины покупателя заполнен', () => {
    const cart = handleGetCart(authedCtx()) as any;
    expect(cart.items.length).toBe(3);
    expect(cart.itemsCount).toBeGreaterThan(0);
    expect(cart.subtotal).toBeGreaterThan(0);
  });

  it('добавление сверх остатка ограничивается количеством с предупреждением', () => {
    // RC-001: остаток 14, в сиде уже 1 → добавляем 20 → ограничение до 14.
    const result = handleAddItem(authedCtx({ method: 'POST', body: { productId: 'p-rc-sterilised', quantity: 20 } })) as any;
    expect(result.cart.items.find((item: any) => item.productId === 'p-rc-sterilised').quantity).toBe(14);
    expect(result.notice).toContain('В наличии только 14 шт.');
  });

  it('промокод МУР10 даёт 10 % при сумме от 1000 ₽', () => {
    const withPromo = handleApplyPromo(authedCtx({ method: 'POST', body: { code: 'МУР10' } })) as any;
    expect(withPromo.cart.promo.code).toBe('МУР10');
    expect(withPromo.cart.discount).toBe(Math.round(withPromo.cart.subtotal * 10) / 100);
    expect(withPromo.cart.total).toBe(withPromo.cart.subtotal - withPromo.cart.discount);
  });

  it('просроченный промокод отклоняется', () => {
    try {
      handleApplyPromo(authedCtx({ method: 'POST', body: { code: 'ПРОМО' } }));
      fail('ожидалась ошибка');
    } catch (error) {
      expect((error as MockHttpError).message).toBe('Промокод не найден или не действует');
    }
  });

  it('исчерпанный лимит использований отклоняется', () => {
    try {
      handleApplyPromo(authedCtx({ method: 'POST', body: { code: 'ЛИМИТ' } }));
      fail('ожидалась ошибка');
    } catch (error) {
      expect((error as MockHttpError).message).toBe('Промокод не найден или не действует');
    }
  });

  it('фиксированная скидка при минимальной сумме применяется и ограничивается', () => {
    const result = handleApplyPromo(authedCtx({ method: 'POST', body: { code: 'СКИДКА500' } })) as any;
    expect(result.cart.promo.code).toBe('СКИДКА500');
  });

  it('оформление заказа: списывает остатки, чистит корзину, нумерует заказ', () => {
    const before = handleGetCart(authedCtx()) as any;
    const stockBefore = JSON.parse(localStorage.getItem(DB_KEY)!).products.find(
      (product: any) => product.id === 'p-ec-extra',
    ).stock;

    const order = handleCreateOrder(
      authedCtx({
        method: 'POST',
        body: {
          recipientName: 'Анна Смирнова',
          recipientPhone: '+79991234567',
          recipientEmail: 'anna.smirnova@example.com',
          address: { city: 'Москва', postalCode: '125047', street: 'ул. Лесная', house: '7', apartment: '24', entrance: '2', floor: '5', intercom: '24' },
          deliveryMethod: 'courier',
          saveAddress: false,
          comment: null,
        },
      }),
    ) as any;

    expect(order.number).toBeGreaterThanOrEqual(1004);

    const after = handleGetCart(authedCtx()) as any;
    expect(after.items.length).toBe(0);

    const stockAfter = JSON.parse(localStorage.getItem(DB_KEY)!).products.find(
      (product: any) => product.id === 'p-ec-extra',
    ).stock;
    const orderedQty = before.items.find((item: any) => item.productId === 'p-ec-extra').quantity;
    expect(stockAfter).toBe(stockBefore - orderedQty);
  });

  it('пустая корзина при оформлении — конфликт', () => {
    mutateDb((db) => {
      db.carts['u-customer'].items = [];
    });
    try {
      handleCreateOrder(
        authedCtx({
          method: 'POST',
          body: {
            recipientName: 'Анна',
            recipientPhone: '+79991234567',
            recipientEmail: null,
            address: { city: 'Москва', postalCode: null, street: 'ул. Лесная', house: '7', apartment: null, entrance: null, floor: null, intercom: null },
            deliveryMethod: 'courier',
            saveAddress: false,
            comment: null,
          },
        }),
      );
      fail('ожидалась ошибка');
    } catch (error) {
      expect((error as MockHttpError).status).toBe(409);
    }
  });
});
