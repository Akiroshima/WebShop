# Спецификация системы «Интернет-магазин»

Версия: 1.0 · Дата: 2026-10-24
Основание: пользовательские истории US-1–US-5 (полные тексты — в §11). Макеты интерфейса отсутствуют — канонические тексты сообщений заданы в §8.

---

## 1. Общие сведения

**Назначение.** Веб-витрина интернет-магазина: покупатель просматривает каталог с фильтрами (цена, категория, бренд), наполняет корзину — в том числе без регистрации, — применяет промокоды и оформляет заказы. Зарегистрированный покупатель хранит в личном кабинете адрес доставки и контактные данные для быстрого оформления; доступ к аккаунту восстанавливается по email или телефону. Менеджер ведёт каталог, промокоды и статусы заказов.

**Тип приложения.** Монолит: одно серверное приложение (ASP.NET Core) обслуживает и REST API, и статику собранного Angular-приложения (SPA). Одна БД PostgreSQL.

**Роли.** `buyer` (покупатель), `manager` (менеджер магазина). Регистрация создаёт только покупателя; учётные записи менеджеров создаёт сид/администратор БД. Права разграничиваются на сервере и в маршрутах SPA.

**Ключевые понятия.**
- *Корзина* — серверная сущность: у гостя привязана к httpOnly-cookie `cart_id`, у авторизованного — к аккаунту; при входе гостевая корзина объединяется с корзиной аккаунта.
- *Промокод* — не более одного на корзину и заказ; скидка в процентах или фиксированная сумма; проверяется при применении и повторно при оформлении заказа.
- *Заказ* — зафиксированный снимок: состав (названия, артикулы, цены), адрес доставки и скидка на момент оформления; после создания у заказа меняется только статус.

**Вне области текущей версии:** онлайн-оплата и расчёт стоимости доставки, полнотекстовый поиск по каталогу, отзывы и рейтинги, избранное и сравнение, иерархия категорий (категории плоские), подтверждение email и телефона при регистрации, загрузка изображений (менеджер указывает URL), мобильные макеты разделов менеджера, интеграционные API (1С, склады, маркетплейсы).

---

## 2. Стек и архитектура

| Слой | Технология |
|---|---|
| Клиент | Angular 20+ (standalone-компоненты, TypeScript strict, SCSS, PrimeNG 20+ — UI-компоненты) |
| Сервер | .NET 10, ASP.NET Core (minimal APIs / controllers), EF Core 10 |
| БД | PostgreSQL 16+, Npgsql, code-first миграции EF Core |
| Аутентификация | JWT в httpOnly-cookie: access (15 мин) + refresh (7 дней) |
| Почта и SMS | SMTP и SMS-шлюз через абстракции `IEmailSender` / `ISmsSender` (в dev — запись в лог) |
| Логирование | Serilog (файл + консоль), health-check `GET /health` |

```
┌──────────────────────────────────────────────┐
│              ASP.NET Core 10 (Kestrel)       │
│  /              → статика Angular (wwwroot)  │
│  /api/v1/...    → REST API (версия в пути)   │
│  /health        → health check               │
└──────────┬───────────────────────┬───────────┘
           │ EF Core 10 (Npgsql)   │ IEmailSender / ISmsSender
     ┌─────┴──────┐         ┌──────┴──────┐
     │ PostgreSQL │         │ SMTP / SMS  │
     └────────────┘         └─────────────┘
```

- **Версионирование API** — в пути: `/api/v1/...`. При несовместимых изменениях добавляется `/api/v2`, v1 поддерживается в переходный период. Клиент зашивает базовый префикс в один interceptor/конфиг.
- SPA fallback: все не-`/api` маршруты отдают `index.html` (deep-link и refresh работают).
- Dev-режим: Angular dev-server с прокси `/api` на Kestrel; Swagger UI только в dev.
- Развёртывание: один процесс приложения + PostgreSQL (docker-compose либо Kestrel за reverse-proxy). Конфигурация через переменные окружения (см. §10).
- **UI-компоненты — PrimeNG** (тема `@primeuix/themes`, пресет Aura, кастомизация через design-tokens):
  - `DataView` (`p-dataview`, layout grid) — товарная сетка каталога с пагинацией «Товары с X по Y из Z» (US-1);
  - `Checkbox` — мультивыбор брендов в фильтре; `Select` (`p-select`) — категория, сортировка каталога, выбор адреса при оформлении, статус заказа у менеджера (US-1, US-4, §4.7);
  - `InputNumber` — цена «от/до» в фильтре и степперы количества в корзине (US-1, US-2); `InputMask` — ввод телефона;
  - `InputText` / `InputPassword` / `Button` — формы входа, регистрации, восстановления доступа, кабинета (US-5, §4.1–4.2, 4.5);
  - `Dialog` — создание/редактирование адреса и формы менеджера; `ConfirmDialog` — удаление адреса, удаления у менеджера (US-3, §4.7);
  - `Toast` — уведомления об успехе и ошибках (§4.9); `Badge` — счётчик товаров на иконке корзины (US-2);
  - `Tabs` (`p-tabs`) — разделы кабинета и раздела «Управление» (US-3, §4.8); `Sidebar` — фильтры каталога на мобильных (§4.10);
  - `Table` (`p-table`) — таблицы менеджера: товары, промокоды, заказы (§4.7).

---

## 3. Ролевая модель

| Возможность | Неавторизованный | Покупатель | Менеджер |
|---|:-:|:-:|:-:|
| Регистрация, вход, восстановление доступа (US-5, §4.1–4.2) | ✔ | — | — |
| Просмотр каталога, фильтры, карточка товара (US-1) | ✔ | ✔ | ✔ |
| Корзина: добавление, изменение количества, удаление (US-2) | ✔ (гостевая по cookie) | ✔ | — |
| Применение промокода к корзине (US-4) | ✔ (гостевая по cookie) | ✔ | — |
| Оформление заказа, «Мои заказы» (US-4) | — (редирект на вход) | ✔ | — |
| Кабинет: профиль, смена пароля | — | ✔ | ✔ |
| Адреса доставки (US-3) | — | ✔ | — |
| Управление каталогом, промокодами, заказами (§4.7) | — | — | ✔ |

- Проверка прав — **на сервере** на каждом эндпоинте (политики авторизации по роли); фронтовые guards — только для UX.
- Роль выдаётся сервером при входе/регистрации, хранится в JWT; SPA строит навигацию по роли из `GET /api/v1/auth/me`.
- Ошибки доступа: 401 (нет/просрочен токен → попытка refresh → редирект на `/login`), 403 (роль не подходит). Чужие заказы и адреса возвращают 404 (существование не раскрывается).

---

## 4. Функциональные требования

### 4.1. Регистрация и аутентификация

**Регистрация** — `POST /api/v1/auth/register`:

- Поля: ФИО, email, телефон (необязательно), пароль, повтор пароля. Роль создаваемой учётной записи — всегда `buyer`.
- Телефон нормализуется к формату E.164 (`+79991234567`); уникальность email и телефона — с понятными сообщениями («Пользователь с таким email уже существует»).
- К паролю — требования из §8. Успех → 201, автоматический вход (ставятся cookie), редирект в каталог; гостевая корзина объединяется с корзиной аккаунта.
- Подтверждение email и телефона не предусмотрено (они используются для входа и восстановления) — зафиксировано решение для v1.
- Анти-спам: не более 5 регистраций в час с одного IP.

**Вход** — `POST /api/v1/auth/login` с `{login, password}`, где login — email **или** телефон:

- При успехе сервер ставит httpOnly-cookie, объединяет гостевую корзину с корзиной аккаунта, возвращает `{role, fullName}`.
- Ошибка любых неправильных данных — единое сообщение **«Неверный логин или пароль»** (не раскрывать существование аккаунта).
- Ограничение частоты: не более 5 попыток в минуту на пару (login, IP) → 429.
- Экраны: десктопная и мобильная вёрстки; на десктопе ошибка — закрываемый баннер, на мобильном — текст под полями.
- Выход: `POST /api/v1/auth/logout` — отзыв refresh-токена, очистка cookie авторизации (cookie корзины гостя остаётся).

**Корзина гостя.** Cookie `cart_id` (httpOnly, TTL `Shop__GuestCartTtlDays` = 30 дней) создаётся при первом действии с корзиной. При объединении одинаковые товары суммируются (не больше остатка); промокод — от гостевой корзины, если задан, иначе от корзины аккаунта.

### 4.2. Восстановление доступа (US-5)

Поток: «Я забыл пароль» → email или телефон → код из письма/SMS → новый пароль.

1. `POST /api/v1/auth/recovery/request {contact}` — контакт: email или телефон, тип распознаётся автоматически. Всегда отвечает 200 (независимо от существования аккаунта). Генерирует 6-значный код, хранит **хэш** кода, отправляет письмом (SMTP) или SMS (шлюз); в dev обе отправки пишутся в лог. TTL кода — 10 минут, одноразовый. Не более 3 запросов в час на контакт.
2. `POST /api/v1/auth/recovery/confirm {contact, code}` — при совпадении возвращает краткоживущий `resetToken` (JWT, TTL 15 минут, назначение `password_reset`). Неверный код → **«Код восстановления не подходит»**; не более 5 попыток на код. На экране — ссылка «Переотправить код».
3. `POST /api/v1/auth/reset-password {resetToken, password, confirmPassword}` — меняет пароль, отзывает все refresh-токены пользователя, редирект на `/login`.

Экран «Новый пароль» (шаг 3): поля «Новый пароль», «Повторите пароль»; требования пароля выведены на экран и проверяются на сервере (§8). Десктопная и мобильная вёрстки.

### 4.3. Каталог и фильтры (US-1)

Главная страница магазина — каталог. `GET /api/v1/catalog/products?category=&brand=&priceMin=&priceMax=&sort=&page=&pageSize=12`:

- Фильтры (панель слева на десктопе, выдвижная `Sidebar` на мобильном):
  - **Категория** — список с количеством товаров, один выбор; по умолчанию «Все категории» (`GET /api/v1/catalog/categories`);
  - **Бренд** — чекбоксы с количеством товаров, мультивыбор; список брендов учитывает выбранную категорию (`GET /api/v1/catalog/brands?category=`);
  - **Цена** — поля «от» и «до» (в рублях).
- Состояние фильтров, сортировка и страница — в query-параметрах URL (deep-link, обновление и шаринг сохраняют выбор).
- Сортировка: «Сначала новые» (по умолчанию), «Цена: по возрастанию», «Цена: по убыванию», «Название: А–Я».
- Товарная сетка 3×4 (12 на страницу, `Shop__CatalogPageSize`): фото, название, бренд, цена; товары без остатка — затемнённые с подписью «Нет в наличии», без кнопки добавления. Ответ `{items, total, page, pageSize}` + подпись «Товары с X по Y из Z», номера страниц и стрелки.
- Учитываются только активные товары (`is_active`).

**Карточка товара** — `GET /api/v1/catalog/products/{id}`: фото, название, бренд (ссылка на каталог с фильтром по этому бренду), категория, описание, цена, наличие («В наличии: N шт.»), степпер количества, кнопка «Добавить в корзину» → Toast «Товар добавлен в корзину», обновление бейджа корзины.

### 4.4. Корзина (US-2)

`GET /api/v1/cart` — гостю по cookie, авторизованному — корзина аккаунта:

- Позиции: фото, название, цена за штуку, количество (степпер 1…остаток), стоимость позиции; кнопка удаления позиции (без подтверждения).
- Цены в корзине — **текущие** (могут измениться после добавления; заказ фиксирует цены на момент оформления — §4.6).
- Действия: добавление `POST /api/v1/cart/items {productId, quantity}` (повторное добавление того же товара увеличивает количество), изменение количества `PUT /api/v1/cart/items/{productId} {quantity}`, удаление `DELETE /api/v1/cart/items/{productId}`.
- Превышение остатка → «В наличии только N шт.» и ограничение количества; добавление неактивного/удалённого товара → «Товара нет в наличии».
- Пустая корзина: «Корзина пуста» + кнопка «Перейти в каталог».
- Итог: «Итого: X ₽» с учётом применённого промокода (§4.6); бейдж на иконке корзины — суммарное количество товаров.

### 4.5. Личный кабинет (US-3)

Раздел «Кабинет» после входа:

**Данные аккаунта** — `GET /api/v1/me/profile` → `{fullName, email, phone}`; `PUT /api/v1/me/profile` — изменение ФИО, email, телефона (уникальность — §8). Смена пароля: `PUT /api/v1/me/password {currentPassword, password, confirmPassword}` — текущий пароль обязателен, новый — по требованиям §8; после смены прочие refresh-токены отзываются.

**Адреса доставки** — `GET/POST /api/v1/me/addresses`, `PUT/DELETE /api/v1/me/addresses/{id}`:

- Карточка адреса: получатель, телефон, город, улица, дом, квартира (необязательно), индекс (необязательно), метка «По умолчанию».
- Один адрес по умолчанию (переключение отдельным действием); не более `Shop__MaxAddressesPerUser` (10).
- Удаление — с подтверждением («Удалить адрес?»); удаление адреса не влияет на оформленные заказы (в них хранится снимок).
- Адреса используются для автозаполнения при оформлении заказа (§4.6) — цель US-3.

**Мои заказы** — `GET /api/v1/orders?page=`, `GET /api/v1/orders/{id}` (только свои, чужие → 404): номер, дата (`дд.мм.гггг`), статус, состав, суммы. Статусы: «Новый», «Подтверждён», «Доставлен», «Отменён». Только чтение.

Экраны кабинета — в десктопной и мобильной вёрстках.

### 4.6. Оформление заказа и промокоды (US-4)

Экран `/checkout` — только для авторизованных; гостю кнопка «Оформить заказ» предлагает войти с возвратом обратно (`/login?returnUrl=/checkout`):

- **Состав заказа** — позиции корзины с текущими ценами.
- **Адрес доставки** — `Select` из сохранённых адресов (по умолчанию — адрес «По умолчанию»); кнопка «+ Новый адрес» открывает `Dialog` (адрес сохраняется в кабинет и сразу выбирается). Получатель и телефон подтягиваются из выбранного адреса.
- **Промокод** — поле + кнопка «Применить»: `POST /api/v1/cart/promo {code}`. Проверка: существует и активен, период действия, минимальная сумма `min_subtotal`, лимит использований `max_uses`. Успех → Toast «Промокод применён» и строка «Промокод CODE · скидка −X ₽» с кнопкой убирания (`DELETE /api/v1/cart/promo`); ошибки — тексты §8. Промокод хранится в корзине; не более одного на заказ.
- **Итоги** — «Товары: S ₽», «Скидка: −D ₽», «Итого: T ₽». Скидка: процент от суммы товаров (округление до копеек) либо фиксированная сумма; скидка не превышает сумму товаров.
- Кнопка **«Оформить заказ»** — `POST /api/v1/orders {addressId, comment?}`. Сервер в одной транзакции: повторно валидирует остатки, цены и промокод; списывает остатки; создаёт заказ со **снимками** названий, артикулов, цен, адреса и промокода; увеличивает `used_count` промокода; очищает корзину. → 201, экран успеха «Заказ №N оформлен».
- Конфликты при оформлении (409): «Некоторые товары стали недоступны», «В наличии только N шт. товара "X"», «Промокод больше не действует» — корзина актуализируется, пользователь остаётся на `/checkout`.

### 4.7. Управление каталогом и заказами — менеджер (обеспечивающий модуль)

Обеспечивающая функциональность (не входит в истории US-1–5): без неё каталог и промокоды неоткуда наполнять. Desktop-first.

- **Товары** (`p-table` с поиском по названию/артикулу + `Dialog`): CRUD — артикул, название, описание, категория, бренд, цена, остаток, URL изображения, признак «активен». Дубликат артикула → 409.
- **Категории и бренды**: CRUD; названия 1–100 символов, уникальны. Удаление категории, в которой есть товары, запрещено (409).
- **Промокоды**: список (код, тип, значение, мин. сумма, период, «использован/лимит», активность) и CRUD: код (верхний регистр), тип («процент»/«фиксированная сумма»), значение, минимальная сумма заказа, начало/окончание, лимит использований (пусто = без лимита), переключатель активности.
- **Заказы**: список с фильтром по статусу и пагинацией; переходы статусов: «Новый» → «Подтверждён» → «Доставлен», «Новый»/«Подтверждён» → «Отменён» (`PUT /api/v1/manage/orders/{id}/status`). При отмене остатки возвращаются на склад.

### 4.8. Оболочка и навигация

- Шапка: логотип, счётчик корзины, меню аккаунта. Гость: «Каталог», «Корзина», «Войти», «Регистрация». Покупатель: «Каталог», «Корзина», «Кабинет» (Данные, Адреса, Заказы, Выход). Менеджер: «Каталог», «Управление» (Товары, Промокоды, Заказы), «Профиль».
- Неавторизованному доступны экраны: «Вход», «Регистрация», «Восстановление пароля».

### 4.9. Уведомления

- Единый компонент (Toast): успех (добавление в корзину, промокод применён, заказ оформлен) и ошибки; закрывается пользователем и по таймауту (5 с).
- Десктоп — всплывающие уведомления (top-right); мобильный — inline-блок под формой для ошибок форм.

### 4.10. Адаптивность

- Breakpoint мобильной вёрстки: `< 768px`.
- Обязательные мобильные макеты: каталог (фильтры — `Sidebar`, сетка 2 колонки), карточка товара, корзина, оформление заказа, кабинет (данные, адреса, заказы), вход, регистрация, восстановление (оба шага), новый пароль.
- Разделы менеджера — desktop-first; на мобильных допускается горизонтальный скролл таблиц.

---

## 5. Модель данных (PostgreSQL)

```sql
CREATE TYPE user_role        AS ENUM ('buyer', 'manager');
CREATE TYPE discount_type    AS ENUM ('percent', 'fixed');
CREATE TYPE order_status     AS ENUM ('new', 'confirmed', 'delivered', 'cancelled');
CREATE TYPE recovery_channel AS ENUM ('email', 'sms');

CREATE TABLE users (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email         varchar(254) NOT NULL,
    phone         varchar(20),                      -- E.164 (+79991234567); null = не указан
    password_hash varchar(500) NOT NULL,            -- PBKDF2-SHA256, 210 000 итераций + соль
    full_name     varchar(200) NOT NULL,
    role          user_role   NOT NULL DEFAULT 'buyer',
    created_at    timestamptz NOT NULL DEFAULT now(),
    UNIQUE (lower(email))
);
CREATE UNIQUE INDEX idx_users_phone ON users(phone) WHERE phone IS NOT NULL;

CREATE TABLE refresh_tokens (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash varchar(500) NOT NULL,
    expires_at timestamptz NOT NULL,
    revoked_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE recovery_codes (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    channel    recovery_channel NOT NULL,
    code_hash  varchar(500) NOT NULL,
    expires_at timestamptz NOT NULL,
    used_at    timestamptz,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE categories (
    id   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(100) NOT NULL,
    UNIQUE (lower(name))
);

CREATE TABLE brands (
    id   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(100) NOT NULL,
    UNIQUE (lower(name))
);

CREATE TABLE products (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    sku         varchar(50)  NOT NULL UNIQUE,
    name        varchar(300) NOT NULL,
    description text,
    category_id uuid NOT NULL REFERENCES categories(id) ON DELETE RESTRICT,
    brand_id    uuid REFERENCES brands(id) ON DELETE SET NULL,
    price       numeric(12,2) NOT NULL CHECK (price >= 0),
    stock       integer NOT NULL DEFAULT 0 CHECK (stock >= 0),
    image_url   varchar(1000),
    is_active   boolean NOT NULL DEFAULT true,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE addresses (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id        uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    recipient_name varchar(200) NOT NULL,
    phone          varchar(20)  NOT NULL,
    city           varchar(100) NOT NULL,
    street         varchar(200) NOT NULL,
    house          varchar(20)  NOT NULL,
    apartment      varchar(20),
    postal_code    varchar(20),
    is_default     boolean NOT NULL DEFAULT false,
    created_at     timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE promo_codes (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code           varchar(50)  NOT NULL,           -- хранится в верхнем регистре
    discount_type  discount_type NOT NULL,
    discount_value numeric(12,2) NOT NULL CHECK (discount_value > 0),
    min_subtotal   numeric(12,2) NOT NULL DEFAULT 0,
    starts_at      timestamptz NOT NULL DEFAULT now(),
    ends_at        timestamptz NOT NULL,
    max_uses       integer,                          -- null = без лимита
    used_count     integer NOT NULL DEFAULT 0,
    is_active      boolean NOT NULL DEFAULT true,
    created_at     timestamptz NOT NULL DEFAULT now(),
    UNIQUE (code),
    CHECK (discount_type <> 'percent' OR discount_value <= 100)
);

CREATE TABLE carts (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id       uuid REFERENCES users(id) ON DELETE CASCADE,   -- null = гостевая (по cookie)
    promo_code_id uuid REFERENCES promo_codes(id) ON DELETE SET NULL,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE cart_items (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cart_id    uuid NOT NULL REFERENCES carts(id) ON DELETE CASCADE,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    quantity   integer NOT NULL CHECK (quantity > 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (cart_id, product_id)
);

CREATE TABLE orders (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    number         integer NOT NULL GENERATED BY DEFAULT AS IDENTITY (START WITH 1000),
    user_id        uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    status         order_status NOT NULL DEFAULT 'new',
    promo_code     varchar(50),                     -- снимок кода; null = без промокода
    subtotal       numeric(12,2) NOT NULL,          -- сумма товаров без скидки
    discount       numeric(12,2) NOT NULL DEFAULT 0,
    total          numeric(12,2) NOT NULL,
    -- снимок адреса доставки на момент оформления:
    recipient_name varchar(200) NOT NULL,
    phone          varchar(20)  NOT NULL,
    city           varchar(100) NOT NULL,
    street         varchar(200) NOT NULL,
    house          varchar(20)  NOT NULL,
    apartment      varchar(20),
    postal_code    varchar(20),
    comment        varchar(500),
    created_at     timestamptz NOT NULL DEFAULT now(),
    UNIQUE (number)
);

CREATE TABLE order_items (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id   uuid NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id uuid REFERENCES products(id) ON DELETE SET NULL,  -- null = товар удалён из каталога
    name       varchar(300) NOT NULL,             -- снимки на момент оформления
    sku        varchar(50)  NOT NULL,
    unit_price numeric(12,2) NOT NULL,
    quantity   integer NOT NULL CHECK (quantity > 0)
);

CREATE INDEX idx_products_filter ON products(is_active, category_id);
CREATE INDEX idx_products_brand   ON products(brand_id);
CREATE INDEX idx_products_price   ON products(price);
CREATE INDEX idx_addresses_user   ON addresses(user_id);
CREATE INDEX idx_cart_items_cart  ON cart_items(cart_id);
CREATE INDEX idx_orders_user      ON orders(user_id, created_at DESC);
CREATE INDEX idx_order_items      ON order_items(order_id);
CREATE UNIQUE INDEX idx_carts_user ON carts(user_id) WHERE user_id IS NOT NULL;      -- одна корзина на аккаунт
CREATE UNIQUE INDEX idx_addresses_default ON addresses(user_id) WHERE is_default;    -- один адрес по умолчанию
```

- Деньги — `numeric(12,2)` (рубли/копейки); отображение «12 345,67 ₽», даты — `дд.мм.гггг`.
- Корзина хранит только ссылки и количества (цены всегда текущие из `products`); заказ — снимки цен и состава.
- Списание остатков и рост `used_count` промокода — в одной транзакции создания заказа; списание атомарное: `UPDATE products SET stock = stock - @q WHERE id = @id AND stock >= @q` (неуспех → 409 «В наличии только N шт.»).
- Начальные данные: сид создаёт менеджера (`Seed__ManagerEmail`, `Seed__ManagerPassword`) и при `Seed__DemoCatalog=true` — демо-каталог (категории, бренды, товары) и промокод `WELCOME10` (10 %).

---

## 6. REST API (`/api/v1`)

Базовый префикс `/api/v1`. Ошибки валидации — HTTP 400 с телом:

```json
{ "message": "Данные заполнены неверно", "errors": { "email": ["Введите корректный email"] } }
```

| Метод и путь | Роль | Назначение |
|---|---|---|
| `POST /api/v1/auth/register` | — | Регистрация покупателя + автоматический вход |
| `POST /api/v1/auth/login` | — | Вход по email или телефону; объединение корзин |
| `POST /api/v1/auth/refresh` | — | Обновление access-токена по refresh-cookie |
| `POST /api/v1/auth/logout` | user | Выход, отзыв refresh-токена |
| `GET /api/v1/auth/me` | user | `{fullName, role}` |
| `POST /api/v1/auth/recovery/request` | — | Код восстановления на email или телефон |
| `POST /api/v1/auth/recovery/confirm` | — | Проверить код → `resetToken` |
| `POST /api/v1/auth/reset-password` | — | Смена пароля по resetToken |
| `GET /api/v1/catalog/categories` | все | Категории с количеством товаров |
| `GET /api/v1/catalog/brands?category=` | все | Бренды (с количеством, с учётом категории) |
| `GET /api/v1/catalog/products?category=&brand=&priceMin=&priceMax=&sort=&page=` | все | Список товаров (фильтры, сортировка, пагинация) |
| `GET /api/v1/catalog/products/{id}` | все | Карточка товара |
| `GET /api/v1/cart` | все | Корзина с текущими ценами, скидкой, итогом |
| `POST /api/v1/cart/items` | все | Добавить товар `{productId, quantity}` |
| `PUT /api/v1/cart/items/{productId}` | все | Изменить количество |
| `DELETE /api/v1/cart/items/{productId}` | все | Удалить позицию |
| `POST /api/v1/cart/promo` | все | Применить промокод `{code}` |
| `DELETE /api/v1/cart/promo` | все | Убрать промокод |
| `POST /api/v1/orders` | buyer | Оформить заказ из корзины `{addressId, comment?}` |
| `GET /api/v1/orders?page=` | buyer | Свои заказы |
| `GET /api/v1/orders/{id}` | buyer | Свой заказ (чужой → 404) |
| `GET /api/v1/me/profile` | user | Данные аккаунта |
| `PUT /api/v1/me/profile` | user | Изменить ФИО, email, телефон |
| `PUT /api/v1/me/password` | user | Смена пароля (текущий + новый) |
| `GET/POST /api/v1/me/addresses` | buyer | Список / создание адреса доставки |
| `PUT/DELETE /api/v1/me/addresses/{id}` | buyer | Изменить / удалить адрес |
| `GET/POST /api/v1/manage/products`, `PUT/DELETE /api/v1/manage/products/{id}` | manager | CRUD товаров |
| `GET/POST /api/v1/manage/categories`, `PUT/DELETE /api/v1/manage/categories/{id}` | manager | CRUD категорий |
| `GET/POST /api/v1/manage/brands`, `PUT/DELETE /api/v1/manage/brands/{id}` | manager | CRUD брендов |
| `GET/POST /api/v1/manage/promos`, `PUT/DELETE /api/v1/manage/promos/{id}` | manager | CRUD промокодов |
| `GET /api/v1/manage/orders?status=&page=` | manager | Заказы магазина |
| `PUT /api/v1/manage/orders/{id}/status` | manager | Смена статуса (при отмене — возврат остатков) |

Коды: 200/201/204 — успех; 400 — ошибка валидации; 401 — не авторизован; 403 — нет прав; 404 — не найдено (в т.ч. чужие заказы/адреса); 409 — конфликт (дубликат email/телефона/артикула/названия/кода промокода, недостаточно остатков, промокод перестал действовать); 429 — превышен лимит попыток.

---

## 7. Маршруты SPA (Angular)

| Маршрут | Экран | Доступ |
|---|---|---|
| `/` | Каталог с фильтрами (US-1) | все |
| `/products/:id` | Карточка товара | все |
| `/cart` | Корзина (US-2) | все |
| `/checkout` | Оформление заказа, промокод (US-4) | buyer |
| `/login` | Вход | все |
| `/register` | Регистрация | все |
| `/recovery` | Восстановление: контакт (US-5) | все |
| `/recovery/code` | Восстановление: код (US-5) | все |
| `/reset-password` | Новый пароль (US-5) | все (по потоку восстановления) |
| `/account` | Кабинет: данные аккаунта, смена пароля | user |
| `/account/addresses` | Адреса доставки (US-3) | buyer |
| `/account/orders`, `/account/orders/:id` | Мои заказы | buyer |
| `/manage/products` | Товары (§4.7) | manager |
| `/manage/promos` | Промокоды (§4.7) | manager |
| `/manage/orders` | Заказы магазина (§4.7) | manager |

- `authGuard` (неавторизованный → `/login?returnUrl=…`), `roleGuard` (чужая роль → 403-страница), `guestGuard` (авторизованный не видит `/login`, `/register`, `/recovery`).
- HTTP-интерцептор: 401 → попытка `POST /api/v1/auth/refresh`, при неудаче — редирект на `/login`; 403/400/409/429 → показ уведомления с `message` из ответа. Базовый префикс `/api/v1` задаётся в одном месте (конфиг клиента).
- State: signals + сервисы поверх HttpClient; состояние фильтров каталога — в URL (Router); отдельный стейт-менеджер не требуется.

---

## 8. Валидации и тексты

| Правило | Сообщение |
|---|---|
| Неверный логин (email/телефон) или пароль | «Неверный логин или пароль» |
| Email не соответствует формату | «Введите корректный email» |
| Телефон не соответствует формату | «Введите корректный номер телефона» |
| Неверный/просроченный код восстановления | «Код восстановления не подходит» |
| Пароль: ≥ 8 символов, ≥ 1 цифра, ≥ 1 буква, ≥ 1 спецзнак; поля совпадают | «Данные заполнены неверно» + список ошибок у полей |
| Занятый email / телефон | «Пользователь с таким email уже существует» / «Пользователь с таким телефоном уже существует» |
| Смена пароля: неверный текущий пароль | «Неверный текущий пароль» |
| Фильтр цены: «от» больше «до» | «Минимальная цена не может превышать максимальную» |
| Количество в корзине больше остатка | «В наличии только N шт.» |
| Добавление недоступного товара | «Товара нет в наличии» |
| Промокод: не найден, неактивен, истёк, исчерпан лимит | «Промокод не найден или не действует» |
| Промокод: сумма меньше минимальной | «Сумма заказа меньше минимальной для этого промокода» |
| Оформление: изменились остатки/состав | «В наличии только N шт. товара "X"» / «Некоторые товары стали недоступны» |
| Адрес: заполнены получатель, телефон, город, улица, дом | «Данные заполнены неверно» + подсветка полей |
| Превышен лимит адресов (10) | «Нельзя сохранить больше 10 адресов» |
| Товар (менеджер): цена ≥ 0, остаток ≥ 0, название 1–300 символов | «Данные заполнены неверно» + подсветка полей |
| Дубликат артикула товара | «Товар с таким артикулом уже существует» |
| Категория/бренд: название 1–100 символов, уникально | «Категория (бренд) с таким названием уже существует» |
| Промокод (менеджер): код уникален, процент ≤ 100, окончание позже начала | «Данные заполнены неверно» / «Промокод с таким кодом уже существует» |

Валидация дублируется на клиенте (UX) и на сервере (истина).

---

## 9. Безопасность

- Пароли: PBKDF2-SHA256, 210 000 итераций, случайная соль (встроенный `Rfc2898DeriveBytes`), никаких обратимых хранилищ.
- Токены: httpOnly + Secure + SameSite=Strict cookie; refresh хранится хэшем и отзывается при logout/смене пароля.
- Восстановление: коды хранятся только хэшем, одноразовые, TTL и лимиты попыток (§4.2); ответы не раскрывают существование контакта.
- **Цены, скидки и итоги считает только сервер** — клиент передаёт идентификаторы и количества; промокод и остатки повторно валидируются при создании заказа; списание остатков атомарно (§5).
- Rate limiting (ASP.NET Core built-in): login, register, recovery-request, recovery-confirm (§4.1–4.2).
- Регистрация создаёт только покупателя; повышение роли невозможно через API — смена ролей отсутствует в v1 вовсе.
- CSRF: SameSite=Strict + отсутствие state-changing GET-эндпоинтов.
- XSS: Angular-санитизация; `image_url` на сервере проверяется на схему http/https.
- Доступ к чужим заказам и адресам — 404, существование не раскрывается; телефон нормализуется и валидируется на сервере (E.164).
- TLS terminates reverse-proxy; Kestrel — только внутренне.
- Все проверки прав — серверные (§3), клиентские guards не считаются защитой.

---

## 10. Нефункциональные требования и конфигурация

- Производительность: p95 API ≤ 500 мс на типовых запросах (объёмы: каталог до 100 000 товаров, корзина до 100 позиций; фильтрация — по индексам §5).
- Локализация интерфейса — русская; валюта — рубль («12 345,67 ₽»), даты `дд.мм.гггг`.
- Логи: Serilog — запросы (метод, путь, статус, время), ошибки; PII (email, телефон) и пароли не логируются.
- `GET /health` — 200 при живом соединении с БД.
- Конфигурация (окружение): `ConnectionStrings__Default`, `Auth__AccessTtlMinutes`, `Auth__RefreshTtlDays`, `Auth__JwtKey`, `Shop__CatalogPageSize` (12), `Shop__MaxAddressesPerUser` (10), `Shop__GuestCartTtlDays` (30), `Smtp__Host/Port/User/Pass/From`, `Sms__ApiUrl/ApiKey/From` (шлюз SMS), `Seed__ManagerEmail/Password`, `Seed__DemoCatalog` (false), `Cors__AllowedOrigins` (только для dev-режима Angular dev-server).

---

## 11. Соответствие историй и требований

| История | Модуль спецификации | Основные эндпоинты / маршруты |
|---|---|---|
| US-1 «просматривать каталог и фильтровать по цене, категории и бренду» | 4.3 | `GET /catalog/categories`, `/catalog/brands`, `/catalog/products`, `/`, `/products/:id` |
| US-2 «добавлять товары в корзину и удалять их из неё» | 4.4 | `GET/POST /cart/items`, `PUT/DELETE /cart/items/{productId}`, `/cart` |
| US-3 «сохранять адрес доставки и контактные данные в личном кабинете» | 4.5 | `/me/addresses*`, `/me/profile`, `/account`, `/account/addresses` |
| US-4 «применять промокоды при оформлении заказа» | 4.6 | `POST/DELETE /cart/promo`, `POST /orders`, `/checkout` |
| US-5 «восстановить доступ по электронной почте или номеру телефона» | 4.2 | `/auth/recovery/*`, `/auth/reset-password`, `/recovery`, `/recovery/code`, `/reset-password` |
| Обеспечивающие: регистрация и вход, кабинет, уведомления, адаптивность, навигация, модуль менеджера | 4.1, 4.5, 4.7–4.10 | `/auth/register`, `/auth/login`, `/manage/*`, сквозные компоненты |
