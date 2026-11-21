-- =====================================================================
-- «Муркет» — 02: схема данных (третья нормальная форма)
--
--   psql -h 127.0.0.1 -p 5432 -U murket -d murket -f 02_schema.sql
--
-- 3НФ: каждая неключевая колонка зависит только от ключа своей таблицы.
--   • цены и остатки товаров живут только в products; корзина хранит лишь
--     ссылки и количества — цена подтягивается при каждом чтении;
--   • счётчики товаров категорий/брендов не хранятся, а вычисляются (COUNT);
--   • заказ — намеренные снимки на момент оформления (название, цена,
--     артикул, адрес, скидка): это исторические факты заказа, а не
--     дублирование справочников;
--   • справочники (категории, бренды, промокоды) отделены от товаров.
-- =====================================================================

BEGIN;

-- ---------- Пользователи ----------
CREATE TABLE IF NOT EXISTS users (
    id            uuid PRIMARY KEY,
    login         text        NOT NULL,
    email         text        NOT NULL,
    phone         text,                      -- E.164 (+79991234567); NULL = не указан
    password_hash text        NOT NULL,      -- PBKDF2-SHA256
    full_name     text        NOT NULL,
    role          text        NOT NULL CHECK (role IN ('customer', 'admin')),
    created_at    timestamptz NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS users_login_ci  ON users (lower(login));
CREATE UNIQUE INDEX IF NOT EXISTS users_email_ci  ON users (lower(email));
CREATE UNIQUE INDEX IF NOT EXISTS users_phone_e164 ON users (phone) WHERE phone IS NOT NULL;

-- ---------- Справочники каталога ----------
CREATE TABLE IF NOT EXISTS categories (
    id   uuid PRIMARY KEY,
    name text NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS brands (
    id   uuid PRIMARY KEY,
    name text NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS products (
    id          uuid           PRIMARY KEY,
    sku         text           NOT NULL,     -- хранится в верхнем регистре
    name        text           NOT NULL,
    description text           NOT NULL,
    category_id uuid           NOT NULL REFERENCES categories (id) ON DELETE RESTRICT,
    brand_id    uuid           REFERENCES brands (id) ON DELETE SET NULL,  -- NULL = без бренда
    price       numeric(12,2)  NOT NULL CHECK (price >= 0),
    stock       integer        NOT NULL CHECK (stock >= 0),
    image_url   text           NOT NULL,
    rating      double precision NOT NULL,
    is_popular  boolean        NOT NULL,
    is_active   boolean        NOT NULL,     -- false = скрыт из каталога покупателей
    created_at  timestamptz    NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS products_sku_ci ON products (upper(sku));
CREATE INDEX IF NOT EXISTS products_category_idx ON products (category_id);
CREATE INDEX IF NOT EXISTS products_brand_idx    ON products (brand_id);
CREATE INDEX IF NOT EXISTS products_price_idx    ON products (price);
CREATE INDEX IF NOT EXISTS products_active_idx   ON products (is_active, category_id);

-- ---------- Промокоды ----------
CREATE TABLE IF NOT EXISTS promos (
    id           uuid          PRIMARY KEY,
    code         text          NOT NULL,     -- хранится в верхнем регистре
    type         text          NOT NULL CHECK (type IN ('percent', 'fixed')),
    value        numeric(12,2) NOT NULL CHECK (value > 0),
    min_subtotal numeric(12,2) NOT NULL CHECK (min_subtotal >= 0),
    starts_at    timestamptz   NOT NULL,
    ends_at      timestamptz   NOT NULL,
    max_uses     integer       CHECK (max_uses IS NULL OR max_uses >= 0),  -- NULL = без лимита
    used_count   integer       NOT NULL CHECK (used_count >= 0),
    is_active    boolean       NOT NULL,
    CHECK (type <> 'percent' OR value <= 100),
    CHECK (ends_at > starts_at)
);
CREATE UNIQUE INDEX IF NOT EXISTS promos_code_ci ON promos (upper(code));

-- ---------- Адреса доставки ----------
CREATE TABLE IF NOT EXISTS addresses (
    id          uuid     PRIMARY KEY,
    user_id     uuid     NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    label       text,                   -- NULL = без метки
    recipient   text     NOT NULL,
    phone       text     NOT NULL,      -- E.164
    city        text     NOT NULL,
    street      text     NOT NULL,
    house       text     NOT NULL,
    apartment   text,
    entrance    text,
    floor       text,
    intercom    text,
    postal_code text,
    is_default  boolean  NOT NULL
);
CREATE INDEX IF NOT EXISTS addresses_user_idx ON addresses (user_id);
CREATE UNIQUE INDEX IF NOT EXISTS addresses_one_default
    ON addresses (user_id) WHERE is_default;             -- один адрес «по умолчанию»

-- ---------- Корзины (одна на пользователя) ----------
CREATE TABLE IF NOT EXISTS carts (
    user_id  uuid PRIMARY KEY REFERENCES users (id) ON DELETE CASCADE,
    promo_id uuid REFERENCES promos (id) ON DELETE SET NULL   -- NULL = промокод не применён
);

CREATE TABLE IF NOT EXISTS cart_items (
    cart_user_id uuid        NOT NULL REFERENCES carts (user_id) ON DELETE CASCADE,
    product_id   uuid        NOT NULL REFERENCES products (id) ON DELETE CASCADE,
    quantity     integer     NOT NULL CHECK (quantity > 0),
    added_at     timestamptz NOT NULL,
    PRIMARY KEY (cart_user_id, product_id)
);
CREATE INDEX IF NOT EXISTS cart_items_product_idx ON cart_items (product_id);

-- ---------- Заказы ----------
-- Номера выдаются sequence; старт 1001 (демо-заказы 1001–1003 сидом).
CREATE SEQUENCE IF NOT EXISTS order_number_seq START 1001;

CREATE TABLE IF NOT EXISTS orders (
    id               uuid         PRIMARY KEY,
    number           integer      NOT NULL UNIQUE,
    user_id          uuid         NOT NULL REFERENCES users (id),
    status           text         NOT NULL CHECK (status IN ('new', 'confirmed', 'delivered', 'cancelled')),
    created_at       timestamptz  NOT NULL,
    recipient_name   text         NOT NULL,   -- снимки на момент оформления
    recipient_phone  text         NOT NULL,
    recipient_email  text,
    comment          text,
    promo_id         uuid         REFERENCES promos (id) ON DELETE SET NULL,  -- NULL = без промокода
    subtotal         numeric(12,2) NOT NULL CHECK (subtotal >= 0),
    discount         numeric(12,2) NOT NULL CHECK (discount >= 0),
    delivery_method  text         NOT NULL CHECK (delivery_method IN ('courier', 'pickup')),
    delivery         numeric(12,2) NOT NULL CHECK (delivery >= 0),
    total            numeric(12,2) NOT NULL CHECK (total >= 0),
    addr_city        text         NOT NULL,   -- снимок адреса доставки
    addr_street      text         NOT NULL,
    addr_house       text         NOT NULL,
    addr_apartment   text,
    addr_entrance    text,
    addr_floor       text,
    addr_intercom    text,
    addr_postal_code text
);
CREATE INDEX IF NOT EXISTS orders_user_created_idx ON orders (user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS orders_status_idx       ON orders (status);

CREATE TABLE IF NOT EXISTS order_items (
    id         uuid         PRIMARY KEY,
    order_id   uuid         NOT NULL REFERENCES orders (id) ON DELETE CASCADE,
    product_id uuid         REFERENCES products (id) ON DELETE SET NULL,  -- NULL = товар удалён из каталога
    name       text         NOT NULL,   -- снимки на момент оформления
    sku        text         NOT NULL,
    price      numeric(12,2) NOT NULL,
    quantity   integer      NOT NULL CHECK (quantity > 0)
);
CREATE INDEX IF NOT EXISTS order_items_order_idx ON order_items (order_id);

-- ---------- Токены безопасности ----------
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id         uuid        PRIMARY KEY,
    user_id    uuid        NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    token_hash text        NOT NULL UNIQUE,   -- SHA-256
    expires_at timestamptz NOT NULL,
    revoked_at timestamptz,                   -- NULL = жив
    created_at timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS refresh_tokens_user_idx ON refresh_tokens (user_id);

CREATE TABLE IF NOT EXISTS recovery_codes (
    id         uuid        PRIMARY KEY,
    user_id    uuid        NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    contact    text        NOT NULL,          -- email (нижний регистр) или телефон E.164
    channel    text        NOT NULL CHECK (channel IN ('email', 'phone')),
    code_hash  text        NOT NULL,          -- солёный SHA-256
    expires_at timestamptz NOT NULL,          -- 10 минут
    used       boolean     NOT NULL,          -- true = использован или сгорел
    attempts   integer     NOT NULL CHECK (attempts >= 0),  -- 5-я ошибка гасит код
    created_at timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS recovery_codes_contact_idx ON recovery_codes (contact);

CREATE TABLE IF NOT EXISTS password_reset_tokens (
    token_hash text        PRIMARY KEY,       -- SHA-256 дайджест
    user_id    uuid        NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    expires_at timestamptz NOT NULL,          -- 15 минут
    used       boolean     NOT NULL,
    created_at timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS password_reset_tokens_user_idx ON password_reset_tokens (user_id);

COMMIT;
