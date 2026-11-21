using MurketApi.Domain.Orders;
using MurketApi.Domain.Promos;
using MurketApi.Domain.Users;

namespace MurketApi.Infrastructure.Seeding;

/// <summary>Параметры сида (только переменные окружения: Seed__*).</summary>
public sealed class SeedOptions
{
    public string AdminLogin { get; set; } = "admin";

    public string? AdminPassword { get; set; }

    public string CustomerLogin { get; set; } = "customer";

    public string? CustomerPassword { get; set; }

    /// <summary>Демо-набор данных (умолчание: Development — да, иначе — нет).</summary>
    public bool? DemoData { get; set; }
}

/// <summary>
/// Демо-данные каталога (идентичны демо-набору 2–3-й лабораторных). Идентификаторы — фиксированные
/// uuid: одни и те же значения используются SQL-скриптом src/api/db/03_seed_demo.sql и сидом
/// приложения, поэтому демо-база воспроизводима любым из двух способов.
/// </summary>
public static class DemoData
{
    public const string CustomerId = "71cb4c04-399d-4bae-a3a2-8142cd327ee6";

    public const string AdminId = "2144ae13-e03f-4b07-bd55-f774da193cb1";

    public const string CustomerEmail = "anna.smirnova@example.com";

    public const string CustomerPhone = "+79991234567";

    public const string HomeAddressId = "0ec7c5ec-022f-4d73-a865-eeb532032df6";

    public const string WorkAddressId = "2695e25f-a041-4035-b4f8-67b327ef13ec";

    public const string FoodCategoryId = "3cc58e99-8c65-4693-bedb-1e3dca45372a";

    public const string LitterCategoryId = "48677f1f-fb17-486a-b4e7-84101aed7c0e";

    public const string ToysCategoryId = "10a557c2-6f76-4e89-9beb-884f9333b669";

    public const string CareCategoryId = "633b71d5-ac9d-4352-9d24-a3f4c6fa53de";

    public const string HomeCategoryId = "59052b2d-d493-427d-b417-c09016b9af23";

    public const string RoyalCaninBrandId = "44d272c9-c1d9-4325-9b56-8732e745f2ad";

    public const string GrandorfBrandId = "ef538d07-6dd1-4ce8-ba7c-a78a4b59dd48";

    public const string MongeBrandId = "db625495-6abc-4445-abbc-c2ac8b3bfe84";

    public const string EverCleanBrandId = "a3218dc0-239e-4331-935a-a5528eebb864";

    public const string TriolBrandId = "05a0edda-81d4-4baa-8900-3ae9ce1c4286";

    public const string BeapharBrandId = "4c659370-0d95-4cdc-8ff1-7f9c113fd67e";

    public static DateTimeOffset D(int day) =>
        new(2026, 10, day, 8, 0, 0, TimeSpan.Zero);

    public static (string Id, string Login, string FullName, string Email, string? Phone, string Password, UserRole Role, DateTimeOffset CreatedAt)[] Users =>
    [
        (CustomerId, "customer", "Анна Смирнова", CustomerEmail, CustomerPhone, "customer123", UserRole.Customer, new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero)),
        (AdminId, "admin", "Админ Муркета", "admin@murket.ru", null, "admin123", UserRole.Admin, new DateTimeOffset(2026, 8, 15, 9, 0, 0, TimeSpan.Zero)),
    ];

    public static (string Id, string Name)[] Categories =>
    [
        (FoodCategoryId, "Корм и лакомства"),
        (LitterCategoryId, "Наполнители"),
        (ToysCategoryId, "Игрушки"),
        (CareCategoryId, "Уход и гигиена"),
        (HomeCategoryId, "Домики и лежанки"),
    ];

    public static (string Id, string Name)[] Brands =>
    [
        (RoyalCaninBrandId, "Royal Canin"),
        (GrandorfBrandId, "Grandorf"),
        (MongeBrandId, "Monge"),
        (EverCleanBrandId, "Ever Clean"),
        (TriolBrandId, "Triol"),
        (BeapharBrandId, "Beaphar"),
    ];

    public static (string Sku, string Id)[] ProductIds =>
    [
        ("RC-001", "4d69d523-af36-4341-9bda-bdb4d238fb09"),
        ("RC-002", "df063a17-de7e-4ba3-85a0-703768ec50ed"),
        ("RC-003", "ddaeb296-d855-497e-880b-ff12feb745d8"),
        ("RC-004", "2f0e66da-6eca-4f58-ab85-696768e605e6"),
        ("RC-005", "a3c92b02-1c67-4e8d-bd14-a4627742a4f5"),
        ("GR-001", "24ed45c7-0290-4248-a927-8fab9b50cc1f"),
        ("GR-002", "b8a28110-77ea-4414-8022-6599baa2ef39"),
        ("GR-003", "3d5356f4-15f3-4ac7-9447-83909bf28143"),
        ("MG-001", "02ff57b4-e5f7-4907-9f9e-5b62739236ab"),
        ("MG-002", "b61ebbef-6622-4af5-9758-b3feebd9d6bc"),
        ("MG-003", "40d8e9e1-63b5-4a01-8193-4cbd2a2e1ab3"),
        ("BP-001", "a9587d35-27af-4bb9-b99a-7f840d7cb1ed"),
        ("TR-001", "9ba88337-53b0-4fa1-9dde-d1188fe4b86d"),
        ("EC-001", "7ed6a7ec-8eb4-4513-9454-930f1819a740"),
        ("EC-002", "330c9e58-0b1a-45aa-9b6d-00414510b56a"),
        ("EC-003", "382fdb73-849b-4cec-9c12-3d0814256c3f"),
        ("EC-004", "02d41078-2721-4a21-bb08-f0e0004a9858"),
        ("TR-002", "d23cf44f-2f02-4c88-bcec-11830bf0a8e7"),
        ("TR-003", "176a0d27-8668-4577-b2c7-30b58b978a35"),
        ("TR-004", "0ae8460f-5fdf-4cc9-9041-fff61064b184"),
        ("TR-005", "31736b16-cbed-4773-9326-3e2d61c550f7"),
        ("TR-006", "75d4591a-9655-48c3-bcba-f70008d05d8c"),
        ("TR-007", "fc250819-3afa-4493-81f4-cb5a7767777b"),
        ("TR-008", "22972476-5d0a-402d-bf86-ac7d09741063"),
        ("TR-009", "ab1b96df-b474-46f4-b2e6-f95d54d30534"),
        ("TR-010", "5a043b43-8d4e-457d-baef-f0c8b9c1d801"),
        ("TR-011", "60d93f72-0b06-485a-9916-3c5ce6491c73"),
        ("TR-012", "b4873de3-9402-49bb-a580-beecbfd2c94a"),
        ("TR-013", "13805dcb-2960-4d16-bf88-4513010075e6"),
        ("TR-014", "2466e2ac-b849-40e5-907e-5a063150d398"),
        ("TR-015", "986aa0fe-d500-439d-99a8-4327af75f4e3"),
        ("TR-016", "35bc9dd1-7daa-4825-8b64-5a5120f9246a"),
        ("BP-002", "2868a357-c024-437f-a879-aea85afcaef1"),
        ("BP-003", "27cc8593-878e-4b22-b26c-9a8bfb772bf5"),
        ("TR-017", "a7fc6533-7aa6-49c6-b14a-cd5d73e27e3c"),
        ("TR-018", "bd7eeb5c-313e-4d29-b28b-a5a6c7054aea"),
        ("TR-019", "f997adc6-b997-44f5-b77f-05b791d04682"),
        ("TR-020", "4bd67b64-592b-4532-b833-59b2c7c7686e"),
        ("TR-021", "8491c7f4-6836-497f-aba3-75d05c85ef6d"),
        ("TR-022", "333f5aa2-aa0d-4976-8c40-559852b5c2e2"),
        ("TR-023", "5e00bffa-5d52-4d06-9bb5-22754ae7e7ae"),
    ];

    /// <summary>Идентификатор демо-товара по артикулу (для тестов и сида).</summary>
    public static string ProductId(string sku) =>
        ProductIds.Single(pair => pair.Sku == sku).Id;

    public sealed record SeedProduct(
        string Sku,
        string Name,
        string Description,
        string CategoryId,
        string BrandId,
        decimal Price,
        int Stock,
        string ImageUrl,
        double Rating,
        bool IsPopular,
        bool IsActive,
        DateTimeOffset CreatedAt);

    private const string Korm = "assets/img/products/korm.svg";
    private const string Konservy = "assets/img/products/konservy.svg";
    private const string Napolnitel = "assets/img/products/napolnitel.svg";
    private const string Mysh = "assets/img/products/mysh.svg";
    private const string Shar = "assets/img/products/shar.svg";
    private const string Tunnel = "assets/img/products/tunnel.svg";
    private const string Draznilka = "assets/img/products/draznilka.svg";
    private const string Shchetka = "assets/img/products/shchetka.svg";
    private const string Lezhanka = "assets/img/products/lezhanka.svg";
    private const string Domik = "assets/img/products/domik.svg";
    private const string Perenoska = "assets/img/products/perenoska.svg";
    private const string Vitamin = "assets/img/products/vitamin.svg";

    public static SeedProduct[] Products =>
    [
        new("RC-001", "Royal Canin Sterilised 37, сухой корм для стерилизованных котов, 2 кг", "Полнорационный корм с контролем веса и поддержкой здоровья мочевыводящей системы.", FoodCategoryId, RoyalCaninBrandId, 2290m, 14, Korm, 4.9, true, true, D(20)),
        new("RC-002", "Royal Canin Persian Adult, сухой корм для персидских котов, 2 кг", "Крокеты миндалевидной формы, созданные для брахицефальных челюстей персов.", FoodCategoryId, RoyalCaninBrandId, 1149m, 18, Korm, 4.7, false, true, D(18)),
        new("RC-003", "Royal Canin Kitten, сухой корм для котят, 2 кг", "Сбалансированный рацион для роста иммунитета котят от 4 до 12 месяцев.", FoodCategoryId, RoyalCaninBrandId, 1890m, 20, Korm, 4.8, false, true, D(17)),
        new("RC-004", "Royal Canin Instinctive, влажный корм в желе, 85 г", "Мягкие кусочки в желе для котов с избирательным аппетитом.", FoodCategoryId, RoyalCaninBrandId, 99m, 80, Konservy, 4.6, false, true, D(16)),
        new("RC-005", "Royal Canin Light Weight Care, 400 г", "Выведен из ассортимента — демо неактивного товара каталога.", FoodCategoryId, RoyalCaninBrandId, 620m, 10, Korm, 4.5, false, false, D(15)),
        new("GR-001", "Grandorf Adult, индейка с рисом, беззерновой корм, 2 кг", "Гипоаллергенный корм из индейки и риса для взрослых котов всех пород.", FoodCategoryId, GrandorfBrandId, 2890m, 10, Korm, 4.9, true, true, D(14)),
        new("GR-002", "Grandorf Weight Control, лосось с клюквой, 2 кг", "Низкокалорийный рацион для котов с склонностью к набору веса.", FoodCategoryId, GrandorfBrandId, 2650m, 8, Korm, 4.7, false, true, D(13)),
        new("GR-003", "Grandorf консервы, индейка и кролик, 95 г", "Влажный корм холистик из мяса индейки и кролика.", FoodCategoryId, GrandorfBrandId, 139m, 0, Konservy, 4.8, false, true, D(12)),
        new("MG-001", "Monge Natural Delight, тунец с лососем, 80 г", "Натуральные консервы без злаков — 70 % мяса рыбы в составе.", FoodCategoryId, MongeBrandId, 129m, 60, Konservy, 4.8, true, true, D(11)),
        new("MG-002", "Monge Cat Natural, тунец, 100 г", "Полнорационный влажный корм с единственным источником белка.", FoodCategoryId, MongeBrandId, 149m, 45, Konservy, 4.6, false, true, D(10)),
        new("MG-003", "Monge All Breed, курица, 1,5 кг", "Сухой корм для взрослых котов с курицей и рисом.", FoodCategoryId, MongeBrandId, 1750m, 12, Korm, 4.5, false, true, D(9)),
        new("BP-001", "Beaphar Duo Active Plus, паста для выведения шерсти, 100 г", "Двухцветная мальт-паста с пребиотиками против комков шерсти.", FoodCategoryId, BeapharBrandId, 520m, 22, Vitamin, 4.7, false, true, D(9)),
        new("TR-001", "Triol лакомство «Мясные подушечки» с курицей, 60 г", "Мягкое лакомство для дрессировки и поощрения кота.", FoodCategoryId, TriolBrandId, 149m, 35, Konservy, 4.4, false, true, D(8)),
        new("EC-001", "Ever Clean Extra Strong, комкующийся наполнитель, 6 л", "Плотные комки и усиленный контроль запаха на срок до 10 дней.", LitterCategoryId, EverCleanBrandId, 890m, 25, Napolnitel, 4.8, true, true, D(20)),
        new("EC-002", "Ever Clean Unscented, без запаха, 10 л", "Гипоаллергенный наполнитель без отдушек для чувствительных котов.", LitterCategoryId, EverCleanBrandId, 1150m, 16, Napolnitel, 4.7, false, true, D(18)),
        new("EC-003", "Ever Clean Lavender, с ароматом лаванды, 6 л", "Комкующийся наполнитель с деликатным ароматом лаванды.", LitterCategoryId, EverCleanBrandId, 960m, 12, Napolnitel, 4.6, false, true, D(17)),
        new("EC-004", "Ever Clean Multiple Cats, для нескольких котов, 10 л", "Максимальный контроль запаха в доме с двумя и более котами.", LitterCategoryId, EverCleanBrandId, 1290m, 9, Napolnitel, 4.8, false, true, D(15)),
        new("TR-002", "Triol древесный наполнитель, 7 л", "Экологичный наполнитель из спрессованных опилок без пыли.", LitterCategoryId, TriolBrandId, 420m, 30, Napolnitel, 4.3, false, true, D(14)),
        new("TR-003", "Triol силикагелевый наполнитель, 3,8 л", "Впитывает влагу и запах, экономичный расход.", LitterCategoryId, TriolBrandId, 890m, 14, Napolnitel, 4.5, false, true, D(13)),
        new("TR-004", "Triol впитывающий наполнитель для котят, 5 л", "Мягкие гранулы для приучения котят к лотку.", LitterCategoryId, TriolBrandId, 380m, 0, Napolnitel, 4.2, false, true, D(12)),
        new("TR-005", "Triol дразнилка «Пёрышко»", "Удочка с натуральным пером — охота без риска для рук хозяина.", ToysCategoryId, TriolBrandId, 390m, 40, Draznilka, 4.8, true, true, D(21)),
        new("TR-006", "Мышка с кошачьей мятой, набор 2 шт", "Мягкие мышки с запасом котовника внутри.", ToysCategoryId, TriolBrandId, 260m, 50, Mysh, 4.6, false, true, D(19)),
        new("TR-007", "Мячики с бубенчиком, набор 3 шт", "Лёгкие мячики для активных игр и апорта.", ToysCategoryId, TriolBrandId, 210m, 45, Shar, 4.4, false, true, D(18)),
        new("TR-008", "Лазерная указка-брелок для игр с котом", "Три режима свечения, класс 2, безопасен для глаз.", ToysCategoryId, TriolBrandId, 450m, 18, Shar, 4.3, false, true, D(16)),
        new("TR-009", "Интерактивная мышь на радиоуправлении", "Бегающая мышь с пультом — движение имитирует настоящую охоту.", ToysCategoryId, TriolBrandId, 1290m, 6, Mysh, 4.7, true, true, D(15)),
        new("TR-010", "Игровой туннель, 90 см", "Шуршащий туннель с дыркой для подглядывания.", ToysCategoryId, TriolBrandId, 890m, 11, Tunnel, 4.6, false, true, D(14)),
        new("TR-011", "Дразнилка-удочка с перьями и колокольчиком", "Телескопическая удочка до 90 см со сменными перьями.", ToysCategoryId, TriolBrandId, 340m, 28, Draznilka, 4.5, false, true, D(13)),
        new("TR-012", "Triol щётка-пуходёрка для котов", "Двухсторонняя щётка: редкие и частые зубья для всех типов шерсти.", CareCategoryId, TriolBrandId, 690m, 30, Shchetka, 4.8, true, true, D(19)),
        new("TR-013", "Triol щётка-рукавица для вычёсывания", "Массажная перчатка собирает шерсть одним движением.", CareCategoryId, TriolBrandId, 450m, 24, Shchetka, 4.5, false, true, D(17)),
        new("TR-014", "Triol шампунь «Нежный» для котов, 250 мл", "Гипоаллергенный шампунь без слёз с протеинами шерсти.", CareCategoryId, TriolBrandId, 380m, 20, Vitamin, 4.4, false, true, D(15)),
        new("TR-015", "Triol зубная паста для котов со вкусом курицы, 100 г", "Паста с энзимами для профилактики зубного камня.", CareCategoryId, TriolBrandId, 350m, 26, Vitamin, 4.3, false, true, D(13)),
        new("TR-016", "Triol ошейник от блох и клещей", "Выведен из продаж — второй демо-пример неактивного товара.", CareCategoryId, TriolBrandId, 590m, 10, Vitamin, 4.2, false, false, D(11)),
        new("BP-002", "Beaphar Ear Cleaner, капли для чистки ушей, 50 мл", "Мягкий лосьон для гигиены ушной раковины.", CareCategoryId, BeapharBrandId, 540m, 15, Vitamin, 4.6, false, true, D(10)),
        new("BP-003", "Beaphar Multi Vitamin, витамины для котов, 180 таблеток", "Комплекс витаминов и таурина для блеска шерсти и иммунитета.", CareCategoryId, BeapharBrandId, 890m, 12, Vitamin, 4.7, false, true, D(9)),
        new("TR-017", "Лежанка «Бублик» велюр, 55 см", "Мягкий бортик-подушка, в котором утонет даже самый пушистый кот.", HomeCategoryId, TriolBrandId, 1590m, 10, Lezhanka, 4.8, true, true, D(18)),
        new("TR-018", "Домик-будка с когтеточкой, 80 см", "Домик из плюша со столбом-когтеточкой из сизаля.", HomeCategoryId, TriolBrandId, 3990m, 5, Domik, 4.9, false, true, D(16)),
        new("TR-019", "Когтеточка напольная «Столбик», 60 см", "Столик с сизалевым столбом и игрушкой-помпоном.", HomeCategoryId, TriolBrandId, 1490m, 8, Domik, 4.6, false, true, D(15)),
        new("TR-020", "Гамак на батарею, 55 × 40 см", "Гамак с меховой подстилкой на радиатор — самое тёплое место дома.", HomeCategoryId, TriolBrandId, 890m, 14, Lezhanka, 4.5, false, true, D(14)),
        new("TR-021", "Лежанка-матрас Triol, 60 × 60 см", "Ровный матрас с антискользящим дном для лотка или пола.", HomeCategoryId, TriolBrandId, 990m, 16, Lezhanka, 4.4, false, true, D(12)),
        new("TR-022", "Домик-лежанка «Капля» со съёмной подушкой", "Закрытый домик-капсула из велюра для укрытия от суеты.", HomeCategoryId, TriolBrandId, 2790m, 4, Domik, 4.7, false, true, D(11)),
        new("TR-023", "Переноска Triol, 59 × 38 × 37 см", "Прочная переноска с металлической дверцей и вентиляцией.", HomeCategoryId, TriolBrandId, 2490m, 7, Perenoska, 4.6, false, true, D(10)),
    ];

    public static (string Id, string UserId, string? Label, string Recipient, string Phone, string City, string Street, string House, string? Apartment, string? Entrance, string? Floor, string? Intercom, string? PostalCode, bool IsDefault)[] Addresses =>
    [
        (HomeAddressId, CustomerId, "Дом", "Анна Смирнова", CustomerPhone, "Москва", "ул. Лесная", "7", "24", "2", "5", "24", "125047", true),
        (WorkAddressId, CustomerId, "Работа", "Анна Смирнова", CustomerPhone, "Москва", "ул. Тверская", "12", "3", null, null, null, "125009", false),
    ];

    public static (string Id, string Code, PromoType Type, decimal Value, decimal MinSubtotal, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int? MaxUses, int UsedCount, bool IsActive)[] Promos =>
    [
        ("aeffdd71-1b47-40c7-99ba-0a95c7145cfa", "МУР10", PromoType.Percent, 10m, 1000m, new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2027, 12, 31, 23, 59, 59, TimeSpan.Zero), null, 3, true),
        ("c5b37f70-7535-4212-a49f-fcaa9b43321e", "СКИДКА500", PromoType.Fixed, 500m, 3500m, new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2027, 6, 30, 23, 59, 59, TimeSpan.Zero), null, 0, true),
        ("a9c16b38-f476-4808-af82-24d346b96d5a", "ПРОМО", PromoType.Percent, 15m, 0m, new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 8, 31, 23, 59, 59, TimeSpan.Zero), null, 12, true),
        ("4cc05dd4-91f3-4b5f-b1f9-ee0e0373f10c", "ЛИМИТ", PromoType.Percent, 5m, 0m, new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2027, 12, 31, 23, 59, 59, TimeSpan.Zero), 10, 10, true),
    ];

    public static (string ProductId, int Quantity, DateTimeOffset AddedAt)[] CartItems =>
    [
        (ProductId("RC-001"), 1, new DateTimeOffset(2026, 10, 22, 9, 10, 0, TimeSpan.Zero)),
        (ProductId("EC-001"), 2, new DateTimeOffset(2026, 10, 22, 9, 12, 0, TimeSpan.Zero)),
        (ProductId("TR-005"), 1, new DateTimeOffset(2026, 10, 23, 18, 40, 0, TimeSpan.Zero)),
    ];

    public sealed record SeedOrder(
        string Id,
        int Number,
        OrderStatus Status,
        DateTimeOffset CreatedAt,
        string? Comment,
        string? PromoCode,
        decimal Subtotal,
        decimal Discount,
        DeliveryMethod DeliveryMethod,
        decimal Delivery,
        (string ProductId, string Name, string Sku, decimal Price, int Quantity)[] Items);

    public static SeedOrder[] Orders =>
    [
        new("8dbcfebd-d83b-4ba6-ada6-aef07c6eb5db", 1001, OrderStatus.Delivered, new DateTimeOffset(2026, 9, 18, 12, 30, 0, TimeSpan.Zero), null, null, 516m, 0m, DeliveryMethod.Courier, 290m,
        [
            (ProductId("MG-001"), "Monge Natural Delight, тунец с лососем, 80 г", "MG-001", 129m, 4),
        ]),
        new("52b7025d-48c6-45ae-96cc-442a74599114", 1002, OrderStatus.Confirmed, new DateTimeOffset(2026, 10, 5, 15, 5, 0, TimeSpan.Zero), "Позвонить за час до доставки", "МУР10", 1780m, 178m, DeliveryMethod.Courier, 290m,
        [
            (ProductId("EC-001"), "Ever Clean Extra Strong, комкующийся наполнитель, 6 л", "EC-001", 890m, 2),
        ]),
        new("e9e18876-b826-463d-ac94-fb39f34ac9a2", 1003, OrderStatus.New, new DateTimeOffset(2026, 10, 21, 10, 45, 0, TimeSpan.Zero), null, null, 1080m, 0m, DeliveryMethod.Pickup, 190m,
        [
            (ProductId("TR-005"), "Triol дразнилка «Пёрышко»", "TR-005", 390m, 1),
            (ProductId("TR-012"), "Triol щётка-пуходёрка для котов", "TR-012", 690m, 1),
        ]),
    ];

    public const int NextOrderNumber = 1004;
}
