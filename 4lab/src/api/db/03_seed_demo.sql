-- =====================================================================
-- «Муркет» — 03: демо-данные каталога (запускать после 02_schema.sql)
--
--   psql -h 127.0.0.1 -p 5432 -U murket -d murket -f 03_seed_demo.sql
--
-- Идемпотентен: повторный запуск не дублирует и не изменяет существующие
-- записи (ON CONFLICT DO NOTHING). Идентификаторы фиксированы и совпадают
-- с сидом приложения (MurketApi.Infrastructure.Seeding.DemoData): база
-- воспроизводима любым из двух способов. Пароли демо-учёток: customer /
-- customer123, admin / admin123 (в Production старт блокирует SEC-001).
-- =====================================================================

BEGIN;
-- ---------- Пользователи (PBKDF2-хэши, 210 000 итераций) ----------
INSERT INTO users (id, login, email, phone, password_hash, full_name, role, created_at) VALUES
('71cb4c04-399d-4bae-a3a2-8142cd327ee6', 'customer', 'anna.smirnova@example.com', '+79991234567', 'pbkdf2-sha256$210000$DjbZC7vE6vZ+8QQ5PCc/6Q==$sl/7bnBIqtg+TiY/UJVs4yEUkR3wqiJ8QEtC/9uptxM=', 'Анна Смирнова', 'customer', '2026-09-01 10:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO users (id, login, email, phone, password_hash, full_name, role, created_at) VALUES
('2144ae13-e03f-4b07-bd55-f774da193cb1', 'admin', 'admin@murket.ru', NULL, 'pbkdf2-sha256$210000$S5BwD+zeap0WrPw+iVDrCQ==$XTR/toWtW8lIbnAH6WhhLJaSdB00oc8hCKalGpRm3j0=', 'Админ Муркета', 'admin', '2026-08-15 09:00:00+00')
ON CONFLICT DO NOTHING;

-- ---------- Справочники каталога ----------
INSERT INTO categories (id, name) VALUES ('3cc58e99-8c65-4693-bedb-1e3dca45372a', 'Корм и лакомства') ON CONFLICT DO NOTHING;
INSERT INTO categories (id, name) VALUES ('48677f1f-fb17-486a-b4e7-84101aed7c0e', 'Наполнители') ON CONFLICT DO NOTHING;
INSERT INTO categories (id, name) VALUES ('10a557c2-6f76-4e89-9beb-884f9333b669', 'Игрушки') ON CONFLICT DO NOTHING;
INSERT INTO categories (id, name) VALUES ('633b71d5-ac9d-4352-9d24-a3f4c6fa53de', 'Уход и гигиена') ON CONFLICT DO NOTHING;
INSERT INTO categories (id, name) VALUES ('59052b2d-d493-427d-b417-c09016b9af23', 'Домики и лежанки') ON CONFLICT DO NOTHING;
INSERT INTO brands (id, name) VALUES ('44d272c9-c1d9-4325-9b56-8732e745f2ad', 'Royal Canin') ON CONFLICT DO NOTHING;
INSERT INTO brands (id, name) VALUES ('ef538d07-6dd1-4ce8-ba7c-a78a4b59dd48', 'Grandorf') ON CONFLICT DO NOTHING;
INSERT INTO brands (id, name) VALUES ('db625495-6abc-4445-abbc-c2ac8b3bfe84', 'Monge') ON CONFLICT DO NOTHING;
INSERT INTO brands (id, name) VALUES ('a3218dc0-239e-4331-935a-a5528eebb864', 'Ever Clean') ON CONFLICT DO NOTHING;
INSERT INTO brands (id, name) VALUES ('05a0edda-81d4-4baa-8900-3ae9ce1c4286', 'Triol') ON CONFLICT DO NOTHING;
INSERT INTO brands (id, name) VALUES ('4c659370-0d95-4cdc-8ff1-7f9c113fd67e', 'Beaphar') ON CONFLICT DO NOTHING;

-- ---------- Товары (41 позиция, 2 неактивные) ----------
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('4d69d523-af36-4341-9bda-bdb4d238fb09', 'RC-001', 'Royal Canin Sterilised 37, сухой корм для стерилизованных котов, 2 кг', 'Полнорационный корм с контролем веса и поддержкой здоровья мочевыводящей системы.',
 '3cc58e99-8c65-4693-bedb-1e3dca45372a', '44d272c9-c1d9-4325-9b56-8732e745f2ad', 2290.00, 14, 'assets/img/products/korm.svg',
 4.9, true, true, '2026-10-20 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('df063a17-de7e-4ba3-85a0-703768ec50ed', 'RC-002', 'Royal Canin Persian Adult, сухой корм для персидских котов, 2 кг', 'Крокеты миндалевидной формы, созданные для брахицефальных челюстей персов.',
 '3cc58e99-8c65-4693-bedb-1e3dca45372a', '44d272c9-c1d9-4325-9b56-8732e745f2ad', 1149.00, 18, 'assets/img/products/korm.svg',
 4.7, false, true, '2026-10-18 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('ddaeb296-d855-497e-880b-ff12feb745d8', 'RC-003', 'Royal Canin Kitten, сухой корм для котят, 2 кг', 'Сбалансированный рацион для роста иммунитета котят от 4 до 12 месяцев.',
 '3cc58e99-8c65-4693-bedb-1e3dca45372a', '44d272c9-c1d9-4325-9b56-8732e745f2ad', 1890.00, 20, 'assets/img/products/korm.svg',
 4.8, false, true, '2026-10-17 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('2f0e66da-6eca-4f58-ab85-696768e605e6', 'RC-004', 'Royal Canin Instinctive, влажный корм в желе, 85 г', 'Мягкие кусочки в желе для котов с избирательным аппетитом.',
 '3cc58e99-8c65-4693-bedb-1e3dca45372a', '44d272c9-c1d9-4325-9b56-8732e745f2ad', 99.00, 80, 'assets/img/products/konservy.svg',
 4.6, false, true, '2026-10-16 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('a3c92b02-1c67-4e8d-bd14-a4627742a4f5', 'RC-005', 'Royal Canin Light Weight Care, 400 г', 'Выведен из ассортимента — демо неактивного товара каталога.',
 '3cc58e99-8c65-4693-bedb-1e3dca45372a', '44d272c9-c1d9-4325-9b56-8732e745f2ad', 620.00, 10, 'assets/img/products/korm.svg',
 4.5, false, false, '2026-10-15 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('24ed45c7-0290-4248-a927-8fab9b50cc1f', 'GR-001', 'Grandorf Adult, индейка с рисом, беззерновой корм, 2 кг', 'Гипоаллергенный корм из индейки и риса для взрослых котов всех пород.',
 '3cc58e99-8c65-4693-bedb-1e3dca45372a', 'ef538d07-6dd1-4ce8-ba7c-a78a4b59dd48', 2890.00, 10, 'assets/img/products/korm.svg',
 4.9, true, true, '2026-10-14 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('b8a28110-77ea-4414-8022-6599baa2ef39', 'GR-002', 'Grandorf Weight Control, лосось с клюквой, 2 кг', 'Низкокалорийный рацион для котов с склонностью к набору веса.',
 '3cc58e99-8c65-4693-bedb-1e3dca45372a', 'ef538d07-6dd1-4ce8-ba7c-a78a4b59dd48', 2650.00, 8, 'assets/img/products/korm.svg',
 4.7, false, true, '2026-10-13 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('3d5356f4-15f3-4ac7-9447-83909bf28143', 'GR-003', 'Grandorf консервы, индейка и кролик, 95 г', 'Влажный корм холистик из мяса индейки и кролика.',
 '3cc58e99-8c65-4693-bedb-1e3dca45372a', 'ef538d07-6dd1-4ce8-ba7c-a78a4b59dd48', 139.00, 0, 'assets/img/products/konservy.svg',
 4.8, false, true, '2026-10-12 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('02ff57b4-e5f7-4907-9f9e-5b62739236ab', 'MG-001', 'Monge Natural Delight, тунец с лососем, 80 г', 'Натуральные консервы без злаков — 70 % мяса рыбы в составе.',
 '3cc58e99-8c65-4693-bedb-1e3dca45372a', 'db625495-6abc-4445-abbc-c2ac8b3bfe84', 129.00, 60, 'assets/img/products/konservy.svg',
 4.8, true, true, '2026-10-11 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('b61ebbef-6622-4af5-9758-b3feebd9d6bc', 'MG-002', 'Monge Cat Natural, тунец, 100 г', 'Полнорационный влажный корм с единственным источником белка.',
 '3cc58e99-8c65-4693-bedb-1e3dca45372a', 'db625495-6abc-4445-abbc-c2ac8b3bfe84', 149.00, 45, 'assets/img/products/konservy.svg',
 4.6, false, true, '2026-10-10 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('40d8e9e1-63b5-4a01-8193-4cbd2a2e1ab3', 'MG-003', 'Monge All Breed, курица, 1,5 кг', 'Сухой корм для взрослых котов с курицей и рисом.',
 '3cc58e99-8c65-4693-bedb-1e3dca45372a', 'db625495-6abc-4445-abbc-c2ac8b3bfe84', 1750.00, 12, 'assets/img/products/korm.svg',
 4.5, false, true, '2026-10-09 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('a9587d35-27af-4bb9-b99a-7f840d7cb1ed', 'BP-001', 'Beaphar Duo Active Plus, паста для выведения шерсти, 100 г', 'Двухцветная мальт-паста с пребиотиками против комков шерсти.',
 '3cc58e99-8c65-4693-bedb-1e3dca45372a', '4c659370-0d95-4cdc-8ff1-7f9c113fd67e', 520.00, 22, 'assets/img/products/vitamin.svg',
 4.7, false, true, '2026-10-09 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('9ba88337-53b0-4fa1-9dde-d1188fe4b86d', 'TR-001', 'Triol лакомство «Мясные подушечки» с курицей, 60 г', 'Мягкое лакомство для дрессировки и поощрения кота.',
 '3cc58e99-8c65-4693-bedb-1e3dca45372a', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 149.00, 35, 'assets/img/products/konservy.svg',
 4.4, false, true, '2026-10-08 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('7ed6a7ec-8eb4-4513-9454-930f1819a740', 'EC-001', 'Ever Clean Extra Strong, комкующийся наполнитель, 6 л', 'Плотные комки и усиленный контроль запаха на срок до 10 дней.',
 '48677f1f-fb17-486a-b4e7-84101aed7c0e', 'a3218dc0-239e-4331-935a-a5528eebb864', 890.00, 25, 'assets/img/products/napolnitel.svg',
 4.8, true, true, '2026-10-20 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('330c9e58-0b1a-45aa-9b6d-00414510b56a', 'EC-002', 'Ever Clean Unscented, без запаха, 10 л', 'Гипоаллергенный наполнитель без отдушек для чувствительных котов.',
 '48677f1f-fb17-486a-b4e7-84101aed7c0e', 'a3218dc0-239e-4331-935a-a5528eebb864', 1150.00, 16, 'assets/img/products/napolnitel.svg',
 4.7, false, true, '2026-10-18 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('382fdb73-849b-4cec-9c12-3d0814256c3f', 'EC-003', 'Ever Clean Lavender, с ароматом лаванды, 6 л', 'Комкующийся наполнитель с деликатным ароматом лаванды.',
 '48677f1f-fb17-486a-b4e7-84101aed7c0e', 'a3218dc0-239e-4331-935a-a5528eebb864', 960.00, 12, 'assets/img/products/napolnitel.svg',
 4.6, false, true, '2026-10-17 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('02d41078-2721-4a21-bb08-f0e0004a9858', 'EC-004', 'Ever Clean Multiple Cats, для нескольких котов, 10 л', 'Максимальный контроль запаха в доме с двумя и более котами.',
 '48677f1f-fb17-486a-b4e7-84101aed7c0e', 'a3218dc0-239e-4331-935a-a5528eebb864', 1290.00, 9, 'assets/img/products/napolnitel.svg',
 4.8, false, true, '2026-10-15 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('d23cf44f-2f02-4c88-bcec-11830bf0a8e7', 'TR-002', 'Triol древесный наполнитель, 7 л', 'Экологичный наполнитель из спрессованных опилок без пыли.',
 '48677f1f-fb17-486a-b4e7-84101aed7c0e', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 420.00, 30, 'assets/img/products/napolnitel.svg',
 4.3, false, true, '2026-10-14 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('176a0d27-8668-4577-b2c7-30b58b978a35', 'TR-003', 'Triol силикагелевый наполнитель, 3,8 л', 'Впитывает влагу и запах, экономичный расход.',
 '48677f1f-fb17-486a-b4e7-84101aed7c0e', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 890.00, 14, 'assets/img/products/napolnitel.svg',
 4.5, false, true, '2026-10-13 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('0ae8460f-5fdf-4cc9-9041-fff61064b184', 'TR-004', 'Triol впитывающий наполнитель для котят, 5 л', 'Мягкие гранулы для приучения котят к лотку.',
 '48677f1f-fb17-486a-b4e7-84101aed7c0e', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 380.00, 0, 'assets/img/products/napolnitel.svg',
 4.2, false, true, '2026-10-12 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('31736b16-cbed-4773-9326-3e2d61c550f7', 'TR-005', 'Triol дразнилка «Пёрышко»', 'Удочка с натуральным пером — охота без риска для рук хозяина.',
 '10a557c2-6f76-4e89-9beb-884f9333b669', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 390.00, 40, 'assets/img/products/draznilka.svg',
 4.8, true, true, '2026-10-21 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('75d4591a-9655-48c3-bcba-f70008d05d8c', 'TR-006', 'Мышка с кошачьей мятой, набор 2 шт', 'Мягкие мышки с запасом котовника внутри.',
 '10a557c2-6f76-4e89-9beb-884f9333b669', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 260.00, 50, 'assets/img/products/mysh.svg',
 4.6, false, true, '2026-10-19 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('fc250819-3afa-4493-81f4-cb5a7767777b', 'TR-007', 'Мячики с бубенчиком, набор 3 шт', 'Лёгкие мячики для активных игр и апорта.',
 '10a557c2-6f76-4e89-9beb-884f9333b669', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 210.00, 45, 'assets/img/products/shar.svg',
 4.4, false, true, '2026-10-18 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('22972476-5d0a-402d-bf86-ac7d09741063', 'TR-008', 'Лазерная указка-брелок для игр с котом', 'Три режима свечения, класс 2, безопасен для глаз.',
 '10a557c2-6f76-4e89-9beb-884f9333b669', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 450.00, 18, 'assets/img/products/shar.svg',
 4.3, false, true, '2026-10-16 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('ab1b96df-b474-46f4-b2e6-f95d54d30534', 'TR-009', 'Интерактивная мышь на радиоуправлении', 'Бегающая мышь с пультом — движение имитирует настоящую охоту.',
 '10a557c2-6f76-4e89-9beb-884f9333b669', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 1290.00, 6, 'assets/img/products/mysh.svg',
 4.7, true, true, '2026-10-15 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('5a043b43-8d4e-457d-baef-f0c8b9c1d801', 'TR-010', 'Игровой туннель, 90 см', 'Шуршащий туннель с дыркой для подглядывания.',
 '10a557c2-6f76-4e89-9beb-884f9333b669', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 890.00, 11, 'assets/img/products/tunnel.svg',
 4.6, false, true, '2026-10-14 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('60d93f72-0b06-485a-9916-3c5ce6491c73', 'TR-011', 'Дразнилка-удочка с перьями и колокольчиком', 'Телескопическая удочка до 90 см со сменными перьями.',
 '10a557c2-6f76-4e89-9beb-884f9333b669', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 340.00, 28, 'assets/img/products/draznilka.svg',
 4.5, false, true, '2026-10-13 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('b4873de3-9402-49bb-a580-beecbfd2c94a', 'TR-012', 'Triol щётка-пуходёрка для котов', 'Двухсторонняя щётка: редкие и частые зубья для всех типов шерсти.',
 '633b71d5-ac9d-4352-9d24-a3f4c6fa53de', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 690.00, 30, 'assets/img/products/shchetka.svg',
 4.8, true, true, '2026-10-19 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('13805dcb-2960-4d16-bf88-4513010075e6', 'TR-013', 'Triol щётка-рукавица для вычёсывания', 'Массажная перчатка собирает шерсть одним движением.',
 '633b71d5-ac9d-4352-9d24-a3f4c6fa53de', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 450.00, 24, 'assets/img/products/shchetka.svg',
 4.5, false, true, '2026-10-17 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('2466e2ac-b849-40e5-907e-5a063150d398', 'TR-014', 'Triol шампунь «Нежный» для котов, 250 мл', 'Гипоаллергенный шампунь без слёз с протеинами шерсти.',
 '633b71d5-ac9d-4352-9d24-a3f4c6fa53de', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 380.00, 20, 'assets/img/products/vitamin.svg',
 4.4, false, true, '2026-10-15 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('986aa0fe-d500-439d-99a8-4327af75f4e3', 'TR-015', 'Triol зубная паста для котов со вкусом курицы, 100 г', 'Паста с энзимами для профилактики зубного камня.',
 '633b71d5-ac9d-4352-9d24-a3f4c6fa53de', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 350.00, 26, 'assets/img/products/vitamin.svg',
 4.3, false, true, '2026-10-13 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('35bc9dd1-7daa-4825-8b64-5a5120f9246a', 'TR-016', 'Triol ошейник от блох и клещей', 'Выведен из продаж — второй демо-пример неактивного товара.',
 '633b71d5-ac9d-4352-9d24-a3f4c6fa53de', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 590.00, 10, 'assets/img/products/vitamin.svg',
 4.2, false, false, '2026-10-11 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('2868a357-c024-437f-a879-aea85afcaef1', 'BP-002', 'Beaphar Ear Cleaner, капли для чистки ушей, 50 мл', 'Мягкий лосьон для гигиены ушной раковины.',
 '633b71d5-ac9d-4352-9d24-a3f4c6fa53de', '4c659370-0d95-4cdc-8ff1-7f9c113fd67e', 540.00, 15, 'assets/img/products/vitamin.svg',
 4.6, false, true, '2026-10-10 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('27cc8593-878e-4b22-b26c-9a8bfb772bf5', 'BP-003', 'Beaphar Multi Vitamin, витамины для котов, 180 таблеток', 'Комплекс витаминов и таурина для блеска шерсти и иммунитета.',
 '633b71d5-ac9d-4352-9d24-a3f4c6fa53de', '4c659370-0d95-4cdc-8ff1-7f9c113fd67e', 890.00, 12, 'assets/img/products/vitamin.svg',
 4.7, false, true, '2026-10-09 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('a7fc6533-7aa6-49c6-b14a-cd5d73e27e3c', 'TR-017', 'Лежанка «Бублик» велюр, 55 см', 'Мягкий бортик-подушка, в котором утонет даже самый пушистый кот.',
 '59052b2d-d493-427d-b417-c09016b9af23', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 1590.00, 10, 'assets/img/products/lezhanka.svg',
 4.8, true, true, '2026-10-18 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('bd7eeb5c-313e-4d29-b28b-a5a6c7054aea', 'TR-018', 'Домик-будка с когтеточкой, 80 см', 'Домик из плюша со столбом-когтеточкой из сизаля.',
 '59052b2d-d493-427d-b417-c09016b9af23', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 3990.00, 5, 'assets/img/products/domik.svg',
 4.9, false, true, '2026-10-16 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('f997adc6-b997-44f5-b77f-05b791d04682', 'TR-019', 'Когтеточка напольная «Столбик», 60 см', 'Столик с сизалевым столбом и игрушкой-помпоном.',
 '59052b2d-d493-427d-b417-c09016b9af23', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 1490.00, 8, 'assets/img/products/domik.svg',
 4.6, false, true, '2026-10-15 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('4bd67b64-592b-4532-b833-59b2c7c7686e', 'TR-020', 'Гамак на батарею, 55 × 40 см', 'Гамак с меховой подстилкой на радиатор — самое тёплое место дома.',
 '59052b2d-d493-427d-b417-c09016b9af23', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 890.00, 14, 'assets/img/products/lezhanka.svg',
 4.5, false, true, '2026-10-14 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('8491c7f4-6836-497f-aba3-75d05c85ef6d', 'TR-021', 'Лежанка-матрас Triol, 60 × 60 см', 'Ровный матрас с антискользящим дном для лотка или пола.',
 '59052b2d-d493-427d-b417-c09016b9af23', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 990.00, 16, 'assets/img/products/lezhanka.svg',
 4.4, false, true, '2026-10-12 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('333f5aa2-aa0d-4976-8c40-559852b5c2e2', 'TR-022', 'Домик-лежанка «Капля» со съёмной подушкой', 'Закрытый домик-капсула из велюра для укрытия от суеты.',
 '59052b2d-d493-427d-b417-c09016b9af23', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 2790.00, 4, 'assets/img/products/domik.svg',
 4.7, false, true, '2026-10-11 08:00:00+00')
ON CONFLICT DO NOTHING;
INSERT INTO products (id, sku, name, description, category_id, brand_id, price, stock, image_url, rating, is_popular, is_active, created_at) VALUES
('5e00bffa-5d52-4d06-9bb5-22754ae7e7ae', 'TR-023', 'Переноска Triol, 59 × 38 × 37 см', 'Прочная переноска с металлической дверцей и вентиляцией.',
 '59052b2d-d493-427d-b417-c09016b9af23', '05a0edda-81d4-4baa-8900-3ae9ce1c4286', 2490.00, 7, 'assets/img/products/perenoska.svg',
 4.6, false, true, '2026-10-10 08:00:00+00')
ON CONFLICT DO NOTHING;

-- ---------- Промокоды ----------
INSERT INTO promos (id, code, type, value, min_subtotal, starts_at, ends_at, max_uses, used_count, is_active) VALUES
('aeffdd71-1b47-40c7-99ba-0a95c7145cfa', 'МУР10', 'percent',
 10.00, 1000.00, '2026-10-01 00:00:00+00', '2027-12-31 23:59:59+00',
 NULL, 3, true)
ON CONFLICT DO NOTHING;
INSERT INTO promos (id, code, type, value, min_subtotal, starts_at, ends_at, max_uses, used_count, is_active) VALUES
('c5b37f70-7535-4212-a49f-fcaa9b43321e', 'СКИДКА500', 'fixed',
 500.00, 3500.00, '2026-10-01 00:00:00+00', '2027-06-30 23:59:59+00',
 NULL, 0, true)
ON CONFLICT DO NOTHING;
INSERT INTO promos (id, code, type, value, min_subtotal, starts_at, ends_at, max_uses, used_count, is_active) VALUES
('a9c16b38-f476-4808-af82-24d346b96d5a', 'ПРОМО', 'percent',
 15.00, 0.00, '2026-05-01 00:00:00+00', '2026-08-31 23:59:59+00',
 NULL, 12, true)
ON CONFLICT DO NOTHING;
INSERT INTO promos (id, code, type, value, min_subtotal, starts_at, ends_at, max_uses, used_count, is_active) VALUES
('4cc05dd4-91f3-4b5f-b1f9-ee0e0373f10c', 'ЛИМИТ', 'percent',
 5.00, 0.00, '2026-09-01 00:00:00+00', '2027-12-31 23:59:59+00',
 '10', 10, true)
ON CONFLICT DO NOTHING;

-- ---------- Адреса покупателя ----------
INSERT INTO addresses (id, user_id, label, recipient, phone, city, street, house, apartment, entrance, floor, intercom, postal_code, is_default) VALUES
('0ec7c5ec-022f-4d73-a865-eeb532032df6', '71cb4c04-399d-4bae-a3a2-8142cd327ee6', 'Дом', 'Анна Смирнова', '+79991234567',
 'Москва', 'ул. Лесная', '7', '24',
 '2', '5', '24', '125047', true)
ON CONFLICT DO NOTHING;
INSERT INTO addresses (id, user_id, label, recipient, phone, city, street, house, apartment, entrance, floor, intercom, postal_code, is_default) VALUES
('2695e25f-a041-4035-b4f8-67b327ef13ec', '71cb4c04-399d-4bae-a3a2-8142cd327ee6', 'Работа', 'Анна Смирнова', '+79991234567',
 'Москва', 'ул. Тверская', '12', '3',
 NULL, NULL, NULL, '125009', false)
ON CONFLICT DO NOTHING;

-- ---------- Корзина покупателя ----------
INSERT INTO carts (user_id, promo_id) VALUES ('71cb4c04-399d-4bae-a3a2-8142cd327ee6', NULL) ON CONFLICT DO NOTHING;
INSERT INTO cart_items (cart_user_id, product_id, quantity, added_at) VALUES
('71cb4c04-399d-4bae-a3a2-8142cd327ee6', '4d69d523-af36-4341-9bda-bdb4d238fb09', 1, '2026-10-22 09:10:00+00') ON CONFLICT DO NOTHING;
INSERT INTO cart_items (cart_user_id, product_id, quantity, added_at) VALUES
('71cb4c04-399d-4bae-a3a2-8142cd327ee6', '7ed6a7ec-8eb4-4513-9454-930f1819a740', 2, '2026-10-22 09:12:00+00') ON CONFLICT DO NOTHING;
INSERT INTO cart_items (cart_user_id, product_id, quantity, added_at) VALUES
('71cb4c04-399d-4bae-a3a2-8142cd327ee6', '31736b16-cbed-4773-9326-3e2d61c550f7', 1, '2026-10-23 18:40:00+00') ON CONFLICT DO NOTHING;

-- ---------- Демо-заказы №1001–1003 (снимки на момент оформления) ----------
INSERT INTO orders (id, number, user_id, status, created_at, recipient_name, recipient_phone,
                    recipient_email, comment, promo_id, subtotal, discount, delivery_method,
                    delivery, total, addr_city, addr_street, addr_house, addr_apartment,
                    addr_entrance, addr_floor, addr_intercom, addr_postal_code) VALUES
('8dbcfebd-d83b-4ba6-ada6-aef07c6eb5db', 1001, '71cb4c04-399d-4bae-a3a2-8142cd327ee6', 'delivered', '2026-09-18 12:30:00+00',
 'Анна Смирнова', '+79991234567', 'anna.smirnova@example.com', NULL,
 NULL, 516.00, 0.00, 'courier',
 290.00, 806.00, 'Москва', 'ул. Лесная', '7', '24', '2', '5', '24', '125047')
ON CONFLICT DO NOTHING;
INSERT INTO order_items (id, order_id, product_id, name, sku, price, quantity) VALUES
('ca44acb8-8000-468e-bceb-57055787ed9c', '8dbcfebd-d83b-4ba6-ada6-aef07c6eb5db', '02ff57b4-e5f7-4907-9f9e-5b62739236ab', 'Monge Natural Delight, тунец с лососем, 80 г', 'MG-001', 129.00, 4)
ON CONFLICT DO NOTHING;
INSERT INTO orders (id, number, user_id, status, created_at, recipient_name, recipient_phone,
                    recipient_email, comment, promo_id, subtotal, discount, delivery_method,
                    delivery, total, addr_city, addr_street, addr_house, addr_apartment,
                    addr_entrance, addr_floor, addr_intercom, addr_postal_code) VALUES
('52b7025d-48c6-45ae-96cc-442a74599114', 1002, '71cb4c04-399d-4bae-a3a2-8142cd327ee6', 'confirmed', '2026-10-05 15:05:00+00',
 'Анна Смирнова', '+79991234567', 'anna.smirnova@example.com', 'Позвонить за час до доставки',
 (SELECT id FROM promos WHERE code = 'МУР10'), 1780.00, 178.00, 'courier',
 290.00, 1892.00, 'Москва', 'ул. Лесная', '7', '24', '2', '5', '24', '125047')
ON CONFLICT DO NOTHING;
INSERT INTO order_items (id, order_id, product_id, name, sku, price, quantity) VALUES
('58f50e41-a9ab-4b19-aa53-66f371000ccb', '52b7025d-48c6-45ae-96cc-442a74599114', '7ed6a7ec-8eb4-4513-9454-930f1819a740', 'Ever Clean Extra Strong, комкующийся наполнитель, 6 л', 'EC-001', 890.00, 2)
ON CONFLICT DO NOTHING;
INSERT INTO orders (id, number, user_id, status, created_at, recipient_name, recipient_phone,
                    recipient_email, comment, promo_id, subtotal, discount, delivery_method,
                    delivery, total, addr_city, addr_street, addr_house, addr_apartment,
                    addr_entrance, addr_floor, addr_intercom, addr_postal_code) VALUES
('e9e18876-b826-463d-ac94-fb39f34ac9a2', 1003, '71cb4c04-399d-4bae-a3a2-8142cd327ee6', 'new', '2026-10-21 10:45:00+00',
 'Анна Смирнова', '+79991234567', 'anna.smirnova@example.com', NULL,
 NULL, 1080.00, 0.00, 'pickup',
 190.00, 1270.00, 'Москва', 'ул. Лесная', '7', '24', '2', '5', '24', '125047')
ON CONFLICT DO NOTHING;
INSERT INTO order_items (id, order_id, product_id, name, sku, price, quantity) VALUES
('7740d2ed-5588-4e68-af43-c48872874d3b', 'e9e18876-b826-463d-ac94-fb39f34ac9a2', '31736b16-cbed-4773-9326-3e2d61c550f7', 'Triol дразнилка «Пёрышко»', 'TR-005', 390.00, 1)
ON CONFLICT DO NOTHING;
INSERT INTO order_items (id, order_id, product_id, name, sku, price, quantity) VALUES
('b53123e8-479b-4021-836d-06175f2d1e0b', 'e9e18876-b826-463d-ac94-fb39f34ac9a2', 'b4873de3-9402-49bb-a580-beecbfd2c94a', 'Triol щётка-пуходёрка для котов', 'TR-012', 690.00, 1)
ON CONFLICT DO NOTHING;

-- Номера следующих заказов — за демо-набором (следующий: 1004).
SELECT setval('order_number_seq', (SELECT max(number) FROM orders));

COMMIT;
