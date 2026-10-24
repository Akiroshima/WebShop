import { Address, DbCart, DbUser, Order, Promo } from '../../shared/models';
import { Category, Brand } from '../../shared/models/catalog';
import { Product } from '../../shared/models/catalog';

import { MockDb } from './mock-db';

/** Идентификаторы сид-данных — используются в тестах. */
export const SEED_VERSION = 1;
export const CUSTOMER_ID = 'u-customer';
export const ADMIN_ID = 'u-admin';

const IMG = {
  korm: 'assets/img/products/korm.svg',
  konservy: 'assets/img/products/konservy.svg',
  napolnitel: 'assets/img/products/napolnitel.svg',
  mysh: 'assets/img/products/mysh.svg',
  shar: 'assets/img/products/shar.svg',
  tunnel: 'assets/img/products/tunnel.svg',
  draznilka: 'assets/img/products/draznilka.svg',
  shchetka: 'assets/img/products/shchetka.svg',
  lezhanka: 'assets/img/products/lezhanka.svg',
  domik: 'assets/img/products/domik.svg',
  perenoska: 'assets/img/products/perenoska.svg',
  vitamin: 'assets/img/products/vitamin.svg',
};

const users: DbUser[] = [
  {
    id: CUSTOMER_ID,
    login: 'customer',
    fullName: 'Анна Смирнова',
    email: 'anna.smirnova@example.com',
    phone: '+79991234567',
    password: 'customer123',
    role: 'customer',
    createdAt: '2026-09-01T10:00:00.000Z',
  },
  {
    id: ADMIN_ID,
    login: 'admin',
    fullName: 'Админ Муркета',
    email: 'admin@murket.ru',
    phone: null,
    password: 'admin123',
    role: 'admin',
    createdAt: '2026-08-15T09:00:00.000Z',
  },
];

const categories: Category[] = [
  { id: 'c-food', name: 'Корм и лакомства' },
  { id: 'c-litter', name: 'Наполнители' },
  { id: 'c-toys', name: 'Игрушки' },
  { id: 'c-care', name: 'Уход и гигиена' },
  { id: 'c-home', name: 'Домики и лежанки' },
];

const brands: Brand[] = [
  { id: 'b-royal-canin', name: 'Royal Canin' },
  { id: 'b-grandorf', name: 'Grandorf' },
  { id: 'b-monge', name: 'Monge' },
  { id: 'b-ever-clean', name: 'Ever Clean' },
  { id: 'b-triol', name: 'Triol' },
  { id: 'b-beaphar', name: 'Beaphar' },
];

interface SeedProduct {
  sku: string;
  name: string;
  description: string;
  categoryId: string;
  brandId: string;
  price: number;
  stock: number;
  imageUrl: string;
  rating: number;
  isPopular: boolean;
  isActive?: boolean;
  createdAt: string;
}

const d = (day: number) => `2026-10-${String(day).padStart(2, '0')}T08:00:00.000Z`;

const products: SeedProduct[] = [
  // Корм и лакомства
  { sku: 'RC-001', name: 'Royal Canin Sterilised 37, сухой корм для стерилизованных котов, 2 кг', description: 'Полнорационный корм с контролем веса и поддержкой здоровья мочевыводящей системы.', categoryId: 'c-food', brandId: 'b-royal-canin', price: 2290, stock: 14, imageUrl: IMG.korm, rating: 4.9, isPopular: true, createdAt: d(20) },
  { sku: 'RC-002', name: 'Royal Canin Persian Adult, сухой корм для персидских котов, 2 кг', description: 'Крокеты миндалевидной формы, созданные для брахицефальных челюстей персов.', categoryId: 'c-food', brandId: 'b-royal-canin', price: 1149, stock: 18, imageUrl: IMG.korm, rating: 4.7, isPopular: false, createdAt: d(18) },
  { sku: 'RC-003', name: 'Royal Canin Kitten, сухой корм для котят, 2 кг', description: 'Сбалансированный рацион для роста иммунитета котят от 4 до 12 месяцев.', categoryId: 'c-food', brandId: 'b-royal-canin', price: 1890, stock: 20, imageUrl: IMG.korm, rating: 4.8, isPopular: false, createdAt: d(17) },
  { sku: 'RC-004', name: 'Royal Canin Instinctive, влажный корм в желе, 85 г', description: 'Мягкие кусочки в желе для котов с избирательным аппетитом.', categoryId: 'c-food', brandId: 'b-royal-canin', price: 99, stock: 80, imageUrl: IMG.konservy, rating: 4.6, isPopular: false, createdAt: d(16) },
  { sku: 'RC-005', name: 'Royal Canin Light Weight Care, 400 г', description: 'Выведен из ассортимента — демо неактивного товара каталога.', categoryId: 'c-food', brandId: 'b-royal-canin', price: 620, stock: 10, imageUrl: IMG.korm, rating: 4.5, isPopular: false, isActive: false, createdAt: d(15) },
  { sku: 'GR-001', name: 'Grandorf Adult, индейка с рисом, беззерновой корм, 2 кг', description: 'Гипоаллергенный корм из индейки и риса для взрослых котов всех пород.', categoryId: 'c-food', brandId: 'b-grandorf', price: 2890, stock: 10, imageUrl: IMG.korm, rating: 4.9, isPopular: true, createdAt: d(14) },
  { sku: 'GR-002', name: 'Grandorf Weight Control, лосось с клюквой, 2 кг', description: 'Низкокалорийный рацион для котов с склонностью к набору веса.', categoryId: 'c-food', brandId: 'b-grandorf', price: 2650, stock: 8, imageUrl: IMG.korm, rating: 4.7, isPopular: false, createdAt: d(13) },
  { sku: 'GR-003', name: 'Grandorf консервы, индейка и кролик, 95 г', description: 'Влажный корм холистик из мяса индейки и кролика.', categoryId: 'c-food', brandId: 'b-grandorf', price: 139, stock: 0, imageUrl: IMG.konservy, rating: 4.8, isPopular: false, createdAt: d(12) },
  { sku: 'MG-001', name: 'Monge Natural Delight, тунец с лососем, 80 г', description: 'Натуральные консервы без злаков — 70 % мяса рыбы в составе.', categoryId: 'c-food', brandId: 'b-monge', price: 129, stock: 60, imageUrl: IMG.konservy, rating: 4.8, isPopular: true, createdAt: d(11) },
  { sku: 'MG-002', name: 'Monge Cat Natural, тунец, 100 г', description: 'Полнорационный влажный корм с единственным источником белка.', categoryId: 'c-food', brandId: 'b-monge', price: 149, stock: 45, imageUrl: IMG.konservy, rating: 4.6, isPopular: false, createdAt: d(10) },
  { sku: 'MG-003', name: 'Monge All Breed, курица, 1,5 кг', description: 'Сухой корм для взрослых котов с курицей и рисом.', categoryId: 'c-food', brandId: 'b-monge', price: 1750, stock: 12, imageUrl: IMG.korm, rating: 4.5, isPopular: false, createdAt: d(9) },
  { sku: 'BP-001', name: 'Beaphar Duo Active Plus, паста для выведения шерсти, 100 г', description: 'Двухцветная мальт-паста с пребиотиками против комков шерсти.', categoryId: 'c-food', brandId: 'b-beaphar', price: 520, stock: 22, imageUrl: IMG.vitamin, rating: 4.7, isPopular: false, createdAt: d(9) },
  { sku: 'TR-001', name: 'Triol лакомство «Мясные подушечки» с курицей, 60 г', description: 'Мягкое лакомство для дрессировки и поощрения кота.', categoryId: 'c-food', brandId: 'b-triol', price: 149, stock: 35, imageUrl: IMG.konservy, rating: 4.4, isPopular: false, createdAt: d(8) },

  // Наполнители
  { sku: 'EC-001', name: 'Ever Clean Extra Strong, комкующийся наполнитель, 6 л', description: 'Плотные комки и усиленный контроль запаха на срок до 10 дней.', categoryId: 'c-litter', brandId: 'b-ever-clean', price: 890, stock: 25, imageUrl: IMG.napolnitel, rating: 4.8, isPopular: true, createdAt: d(20) },
  { sku: 'EC-002', name: 'Ever Clean Unscented, без запаха, 10 л', description: 'Гипоаллергенный наполнитель без отдушек для чувствительных котов.', categoryId: 'c-litter', brandId: 'b-ever-clean', price: 1150, stock: 16, imageUrl: IMG.napolnitel, rating: 4.7, isPopular: false, createdAt: d(18) },
  { sku: 'EC-003', name: 'Ever Clean Lavender, с ароматом лаванды, 6 л', description: 'Комкующийся наполнитель с деликатным ароматом лаванды.', categoryId: 'c-litter', brandId: 'b-ever-clean', price: 960, stock: 12, imageUrl: IMG.napolnitel, rating: 4.6, isPopular: false, createdAt: d(17) },
  { sku: 'EC-004', name: 'Ever Clean Multiple Cats, для нескольких котов, 10 л', description: 'Максимальный контроль запаха в доме с двумя и более котами.', categoryId: 'c-litter', brandId: 'b-ever-clean', price: 1290, stock: 9, imageUrl: IMG.napolnitel, rating: 4.8, isPopular: false, createdAt: d(15) },
  { sku: 'TR-002', name: 'Triol древесный наполнитель, 7 л', description: 'Экологичный наполнитель из спрессованных опилок без пыли.', categoryId: 'c-litter', brandId: 'b-triol', price: 420, stock: 30, imageUrl: IMG.napolnitel, rating: 4.3, isPopular: false, createdAt: d(14) },
  { sku: 'TR-003', name: 'Triol силикагелевый наполнитель, 3,8 л', description: 'Впитывает влагу и запах, экономичный расход.', categoryId: 'c-litter', brandId: 'b-triol', price: 890, stock: 14, imageUrl: IMG.napolnitel, rating: 4.5, isPopular: false, createdAt: d(13) },
  { sku: 'TR-004', name: 'Triol впитывающий наполнитель для котят, 5 л', description: 'Мягкие гранулы для приучения котят к лотку.', categoryId: 'c-litter', brandId: 'b-triol', price: 380, stock: 0, imageUrl: IMG.napolnitel, rating: 4.2, isPopular: false, createdAt: d(12) },

  // Игрушки
  { sku: 'TR-005', name: 'Triol дразнилка «Пёрышко»', description: 'Удочка с натуральным пером — охота без риска для рук хозяина.', categoryId: 'c-toys', brandId: 'b-triol', price: 390, stock: 40, imageUrl: IMG.draznilka, rating: 4.8, isPopular: true, createdAt: d(21) },
  { sku: 'TR-006', name: 'Мышка с кошачьей мятой, набор 2 шт', description: 'Мягкие мышки с запасом котовника внутри.', categoryId: 'c-toys', brandId: 'b-triol', price: 260, stock: 50, imageUrl: IMG.mysh, rating: 4.6, isPopular: false, createdAt: d(19) },
  { sku: 'TR-007', name: 'Мячики с бубенчиком, набор 3 шт', description: 'Лёгкие мячики для активных игр и апорта.', categoryId: 'c-toys', brandId: 'b-triol', price: 210, stock: 45, imageUrl: IMG.shar, rating: 4.4, isPopular: false, createdAt: d(18) },
  { sku: 'TR-008', name: 'Лазерная указка-брелок для игр с котом', description: 'Три режима свечения, класс 2, безопасен для глаз.', categoryId: 'c-toys', brandId: 'b-triol', price: 450, stock: 18, imageUrl: IMG.shar, rating: 4.3, isPopular: false, createdAt: d(16) },
  { sku: 'TR-009', name: 'Интерактивная мышь на радиоуправлении', description: 'Бегающая мышь с пультом — движение имитирует настоящую охоту.', categoryId: 'c-toys', brandId: 'b-triol', price: 1290, stock: 6, imageUrl: IMG.mysh, rating: 4.7, isPopular: true, createdAt: d(15) },
  { sku: 'TR-010', name: 'Игровой туннель, 90 см', description: 'Шуршащий туннель с дыркой для подглядывания.', categoryId: 'c-toys', brandId: 'b-triol', price: 890, stock: 11, imageUrl: IMG.tunnel, rating: 4.6, isPopular: false, createdAt: d(14) },
  { sku: 'TR-011', name: 'Дразнилка-удочка с перьями и колокольчиком', description: 'Телескопическая удочка до 90 см со сменными перьями.', categoryId: 'c-toys', brandId: 'b-triol', price: 340, stock: 28, imageUrl: IMG.draznilka, rating: 4.5, isPopular: false, createdAt: d(13) },

  // Уход и гигиена
  { sku: 'TR-012', name: 'Triol щётка-пуходёрка для котов', description: 'Двухсторонняя щётка: редкие и частые зубья для всех типов шерсти.', categoryId: 'c-care', brandId: 'b-triol', price: 690, stock: 30, imageUrl: IMG.shchetka, rating: 4.8, isPopular: true, createdAt: d(19) },
  { sku: 'TR-013', name: 'Triol щётка-рукавица для вычёсывания', description: 'Массажная перчатка собирает шерсть одним движением.', categoryId: 'c-care', brandId: 'b-triol', price: 450, stock: 24, imageUrl: IMG.shchetka, rating: 4.5, isPopular: false, createdAt: d(17) },
  { sku: 'TR-014', name: 'Triol шампунь «Нежный» для котов, 250 мл', description: 'Гипоаллергенный шампунь без слёз с протеинами шерсти.', categoryId: 'c-care', brandId: 'b-triol', price: 380, stock: 20, imageUrl: IMG.vitamin, rating: 4.4, isPopular: false, createdAt: d(15) },
  { sku: 'TR-015', name: 'Triol зубная паста для котов со вкусом курицы, 100 г', description: 'Паста с энзимами для профилактики зубного камня.', categoryId: 'c-care', brandId: 'b-triol', price: 350, stock: 26, imageUrl: IMG.vitamin, rating: 4.3, isPopular: false, createdAt: d(13) },
  { sku: 'TR-016', name: 'Triol ошейник от блох и клещей', description: 'Выведен из продаж — второй демо-пример неактивного товара.', categoryId: 'c-care', brandId: 'b-triol', price: 590, stock: 10, imageUrl: IMG.vitamin, rating: 4.2, isPopular: false, isActive: false, createdAt: d(11) },
  { sku: 'BP-002', name: 'Beaphar Ear Cleaner, капли для чистки ушей, 50 мл', description: 'Мягкий лосьон для гигиены ушной раковины.', categoryId: 'c-care', brandId: 'b-beaphar', price: 540, stock: 15, imageUrl: IMG.vitamin, rating: 4.6, isPopular: false, createdAt: d(10) },
  { sku: 'BP-003', name: 'Beaphar Multi Vitamin, витамины для котов, 180 таблеток', description: 'Комплекс витаминов и таурина для блеска шерсти и иммунитета.', categoryId: 'c-care', brandId: 'b-beaphar', price: 890, stock: 12, imageUrl: IMG.vitamin, rating: 4.7, isPopular: false, createdAt: d(9) },

  // Домики и лежанки
  { sku: 'TR-017', name: 'Лежанка «Бублик» велюр, 55 см', description: 'Мягкий бортик-подушка, в котором утонет даже самый пушистый кот.', categoryId: 'c-home', brandId: 'b-triol', price: 1590, stock: 10, imageUrl: IMG.lezhanka, rating: 4.8, isPopular: true, createdAt: d(18) },
  { sku: 'TR-018', name: 'Домик-будка с когтеточкой, 80 см', description: 'Домик из плюша со столбом-когтеточкой из сизаля.', categoryId: 'c-home', brandId: 'b-triol', price: 3990, stock: 5, imageUrl: IMG.domik, rating: 4.9, isPopular: false, createdAt: d(16) },
  { sku: 'TR-019', name: 'Когтеточка напольная «Столбик», 60 см', description: 'Столик с сизалевым столбом и игрушкой-помпоном.', categoryId: 'c-home', brandId: 'b-triol', price: 1490, stock: 8, imageUrl: IMG.domik, rating: 4.6, isPopular: false, createdAt: d(15) },
  { sku: 'TR-020', name: 'Гамак на батарею, 55 × 40 см', description: 'Гамак с меховой подстилкой на радиатор — самое тёплое место дома.', categoryId: 'c-home', brandId: 'b-triol', price: 890, stock: 14, imageUrl: IMG.lezhanka, rating: 4.5, isPopular: false, createdAt: d(14) },
  { sku: 'TR-021', name: 'Лежанка-матрас Triol, 60 × 60 см', description: 'Ровный матрас с антискользящим дном для лотка или пола.', categoryId: 'c-home', brandId: 'b-triol', price: 990, stock: 16, imageUrl: IMG.lezhanka, rating: 4.4, isPopular: false, createdAt: d(12) },
  { sku: 'TR-022', name: 'Домик-лежанка «Капля» со съёмной подушкой', description: 'Закрытый домик-капсула из велюра для укрытия от суеты.', categoryId: 'c-home', brandId: 'b-triol', price: 2790, stock: 4, imageUrl: IMG.domik, rating: 4.7, isPopular: false, createdAt: d(11) },
  { sku: 'TR-023', name: 'Переноска Triol, 59 × 38 × 37 см', description: 'Прочная переноска с металлической дверцей и вентиляцией.', categoryId: 'c-home', brandId: 'b-triol', price: 2490, stock: 7, imageUrl: IMG.perenoska, rating: 4.6, isPopular: false, createdAt: d(10) },
];

const addresses: Address[] = [
  {
    id: 'a-home',
    userId: CUSTOMER_ID,
    label: 'Дом',
    recipient: 'Анна Смирнова',
    phone: '+79991234567',
    city: 'Москва',
    street: 'ул. Лесная',
    house: '7',
    apartment: '24',
    entrance: '2',
    floor: '5',
    intercom: '24',
    postalCode: '125047',
    isDefault: true,
  },
  {
    id: 'a-work',
    userId: CUSTOMER_ID,
    label: 'Работа',
    recipient: 'Анна Смирнова',
    phone: '+79991234567',
    city: 'Москва',
    street: 'ул. Тверская',
    house: '12',
    apartment: '3',
    entrance: null,
    floor: null,
    intercom: null,
    postalCode: '125009',
    isDefault: false,
  },
];

const promos: Promo[] = [
  {
    id: 'p-mur10',
    code: 'МУР10',
    type: 'percent',
    value: 10,
    minSubtotal: 1000,
    startsAt: '2026-10-01T00:00:00.000Z',
    endsAt: '2027-12-31T23:59:59.000Z',
    maxUses: null,
    usedCount: 3,
    isActive: true,
  },
  {
    id: 'p-500',
    code: 'СКИДКА500',
    type: 'fixed',
    value: 500,
    minSubtotal: 3500,
    startsAt: '2026-10-01T00:00:00.000Z',
    endsAt: '2027-06-30T23:59:59.000Z',
    maxUses: null,
    usedCount: 0,
    isActive: true,
  },
  {
    id: 'p-old',
    code: 'ПРОМО',
    type: 'percent',
    value: 15,
    minSubtotal: 0,
    startsAt: '2026-05-01T00:00:00.000Z',
    endsAt: '2026-08-31T23:59:59.000Z',
    maxUses: null,
    usedCount: 12,
    isActive: true,
  },
  {
    id: 'p-limit',
    code: 'ЛИМИТ',
    type: 'percent',
    value: 5,
    minSubtotal: 0,
    startsAt: '2026-09-01T00:00:00.000Z',
    endsAt: '2027-12-31T23:59:59.000Z',
    maxUses: 10,
    usedCount: 10,
    isActive: true,
  },
];

const carts: Record<string, DbCart> = {
  [CUSTOMER_ID]: {
    userId: CUSTOMER_ID,
    promoCode: null,
    items: [
      { productId: 'p-rc-sterilised', quantity: 1, addedAt: '2026-10-22T09:10:00.000Z' },
      { productId: 'p-ec-extra', quantity: 2, addedAt: '2026-10-22T09:12:00.000Z' },
      { productId: 'p-tr-feather', quantity: 1, addedAt: '2026-10-23T18:40:00.000Z' },
    ],
  },
};

const orders: Order[] = [
  {
    id: 'o-1001',
    number: 1001,
    userId: CUSTOMER_ID,
    status: 'delivered',
    createdAt: '2026-09-18T12:30:00.000Z',
    recipientName: 'Анна Смирнова',
    recipientPhone: '+79991234567',
    recipientEmail: 'anna.smirnova@example.com',
    address: { city: 'Москва', postalCode: '125047', street: 'ул. Лесная', house: '7', apartment: '24', entrance: '2', floor: '5', intercom: '24' },
    comment: null,
    promoCode: null,
    subtotal: 516,
    discount: 0,
    deliveryMethod: 'courier',
    delivery: 290,
    total: 806,
    items: [{ productId: 'p-mg-tuna', name: 'Monge Natural Delight, тунец с лососем, 80 г', sku: 'MG-001', price: 129, quantity: 4 }],
  },
  {
    id: 'o-1002',
    number: 1002,
    userId: CUSTOMER_ID,
    status: 'confirmed',
    createdAt: '2026-10-05T15:05:00.000Z',
    recipientName: 'Анна Смирнова',
    recipientPhone: '+79991234567',
    recipientEmail: 'anna.smirnova@example.com',
    address: { city: 'Москва', postalCode: '125047', street: 'ул. Лесная', house: '7', apartment: '24', entrance: '2', floor: '5', intercom: '24' },
    comment: 'Позвонить за час до доставки',
    promoCode: 'МУР10',
    subtotal: 1780,
    discount: 178,
    deliveryMethod: 'courier',
    delivery: 290,
    total: 1892,
    items: [{ productId: 'p-ec-extra', name: 'Ever Clean Extra Strong, комкующийся наполнитель, 6 л', sku: 'EC-001', price: 890, quantity: 2 }],
  },
  {
    id: 'o-1003',
    number: 1003,
    userId: CUSTOMER_ID,
    status: 'new',
    createdAt: '2026-10-21T10:45:00.000Z',
    recipientName: 'Анна Смирнова',
    recipientPhone: '+79991234567',
    recipientEmail: 'anna.smirnova@example.com',
    address: { city: 'Москва', postalCode: '125047', street: 'ул. Лесная', house: '7', apartment: '24', entrance: '2', floor: '5', intercom: '24' },
    comment: null,
    promoCode: null,
    subtotal: 1080,
    discount: 0,
    deliveryMethod: 'pickup',
    delivery: 190,
    total: 1270,
    items: [
      { productId: 'p-tr-feather', name: 'Triol дразнилка «Пёрышко»', sku: 'TR-005', price: 390, quantity: 1 },
      { productId: 'p-tr-brush', name: 'Triol щётка-пуходёрка для котов', sku: 'TR-012', price: 690, quantity: 1 },
    ],
  },
];

const PRODUCT_IDS: Record<string, string> = {
  'RC-001': 'p-rc-sterilised',
  'RC-002': 'p-rc-persian',
  'RC-003': 'p-rc-kitten',
  'RC-004': 'p-rc-instinctive',
  'RC-005': 'p-rc-light',
  'GR-001': 'p-gr-turkey',
  'GR-002': 'p-gr-salmon',
  'GR-003': 'p-gr-canned',
  'MG-001': 'p-mg-tuna',
  'MG-002': 'p-mg-natural',
  'MG-003': 'p-mg-chicken',
  'BP-001': 'p-bp-pasta',
  'TR-001': 'p-tr-treats',
  'EC-001': 'p-ec-extra',
  'EC-002': 'p-ec-unscented',
  'EC-003': 'p-ec-lavender',
  'EC-004': 'p-ec-multi',
  'TR-002': 'p-tr-wood',
  'TR-003': 'p-tr-silica',
  'TR-004': 'p-tr-absorb',
  'TR-005': 'p-tr-feather',
  'TR-006': 'p-tr-mouse',
  'TR-007': 'p-tr-balls',
  'TR-008': 'p-tr-laser',
  'TR-009': 'p-tr-rcmouse',
  'TR-010': 'p-tr-tunnel',
  'TR-011': 'p-tr-rod',
  'TR-012': 'p-tr-brush',
  'TR-013': 'p-tr-glove',
  'TR-014': 'p-tr-shampoo',
  'TR-015': 'p-tr-toothpaste',
  'TR-016': 'p-tr-collar',
  'BP-002': 'p-bp-ears',
  'BP-003': 'p-bp-vitamins',
  'TR-017': 'p-tr-bed',
  'TR-018': 'p-tr-house',
  'TR-019': 'p-tr-scratcher',
  'TR-020': 'p-tr-hammock',
  'TR-021': 'p-tr-mattress',
  'TR-022': 'p-tr-drop',
  'TR-023': 'p-tr-carrier',
};

export function seedDb(): MockDb {
  return {
    seedVersion: SEED_VERSION,
    users: structuredClone(users),
    categories: structuredClone(categories),
    brands: structuredClone(brands),
    products: products.map((p) => ({ ...p, id: PRODUCT_IDS[p.sku] ?? `p-${p.sku.toLowerCase()}`, isActive: p.isActive ?? true })),
    addresses: structuredClone(addresses),
    promos: structuredClone(promos),
    carts: structuredClone(carts),
    orders: structuredClone(orders),
    nextOrderNumber: 1004,
    recoveryRequests: [],
    resetTokens: [],
  };
}
