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

/// <summary>Демо-данные каталога (идентичны демо-набору 2-й лабораторной).</summary>
public static class DemoData
{
    public const string CustomerId = "u-customer";

    public const string AdminId = "u-admin";

    public const string CustomerEmail = "anna.smirnova@example.com";

    public const string CustomerPhone = "+79991234567";

    public static DateTimeOffset D(int day) =>
        new(2026, 10, day, 8, 0, 0, TimeSpan.Zero);

    public static (string Id, string Login, string FullName, string Email, string? Phone, string Password, UserRole Role, DateTimeOffset CreatedAt)[] Users =>
    [
        (CustomerId, "customer", "Анна Смирнова", CustomerEmail, CustomerPhone, "customer123", UserRole.Customer, new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero)),
        (AdminId, "admin", "Админ Муркета", "admin@murket.ru", null, "admin123", UserRole.Admin, new DateTimeOffset(2026, 8, 15, 9, 0, 0, TimeSpan.Zero)),
    ];

    public static (string Id, string Name)[] Categories =>
    [
        ("c-food", "Корм и лакомства"),
        ("c-litter", "Наполнители"),
        ("c-toys", "Игрушки"),
        ("c-care", "Уход и гигиена"),
        ("c-home", "Домики и лежанки"),
    ];

    public static (string Id, string Name)[] Brands =>
    [
        ("b-royal-canin", "Royal Canin"),
        ("b-grandorf", "Grandorf"),
        ("b-monge", "Monge"),
        ("b-ever-clean", "Ever Clean"),
        ("b-triol", "Triol"),
        ("b-beaphar", "Beaphar"),
    ];

    public static (string Sku, string Id)[] ProductIds =>
    [
        ("RC-001", "p-rc-sterilised"),
        ("RC-002", "p-rc-persian"),
        ("RC-003", "p-rc-kitten"),
        ("RC-004", "p-rc-instinctive"),
        ("RC-005", "p-rc-light"),
        ("GR-001", "p-gr-turkey"),
        ("GR-002", "p-gr-salmon"),
        ("GR-003", "p-gr-canned"),
        ("MG-001", "p-mg-tuna"),
        ("MG-002", "p-mg-natural"),
        ("MG-003", "p-mg-chicken"),
        ("BP-001", "p-bp-pasta"),
        ("TR-001", "p-tr-treats"),
        ("EC-001", "p-ec-extra"),
        ("EC-002", "p-ec-unscented"),
        ("EC-003", "p-ec-lavender"),
        ("EC-004", "p-ec-multi"),
        ("TR-002", "p-tr-wood"),
        ("TR-003", "p-tr-silica"),
        ("TR-004", "p-tr-absorb"),
        ("TR-005", "p-tr-feather"),
        ("TR-006", "p-tr-mouse"),
        ("TR-007", "p-tr-balls"),
        ("TR-008", "p-tr-laser"),
        ("TR-009", "p-tr-rcmouse"),
        ("TR-010", "p-tr-tunnel"),
        ("TR-011", "p-tr-rod"),
        ("TR-012", "p-tr-brush"),
        ("TR-013", "p-tr-glove"),
        ("TR-014", "p-tr-shampoo"),
        ("TR-015", "p-tr-toothpaste"),
        ("TR-016", "p-tr-collar"),
        ("BP-002", "p-bp-ears"),
        ("BP-003", "p-bp-vitamins"),
        ("TR-017", "p-tr-bed"),
        ("TR-018", "p-tr-house"),
        ("TR-019", "p-tr-scratcher"),
        ("TR-020", "p-tr-hammock"),
        ("TR-021", "p-tr-mattress"),
        ("TR-022", "p-tr-drop"),
        ("TR-023", "p-tr-carrier"),
    ];

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
        new("RC-001", "Royal Canin Sterilised 37, сухой корм для стерилизованных котов, 2 кг", "Полнорационный корм с контролем веса и поддержкой здоровья мочевыводящей системы.", "c-food", "b-royal-canin", 2290m, 14, Korm, 4.9, true, true, D(20)),
        new("RC-002", "Royal Canin Persian Adult, сухой корм для персидских котов, 2 кг", "Крокеты миндалевидной формы, созданные для брахицефальных челюстей персов.", "c-food", "b-royal-canin", 1149m, 18, Korm, 4.7, false, true, D(18)),
        new("RC-003", "Royal Canin Kitten, сухой корм для котят, 2 кг", "Сбалансированный рацион для роста иммунитета котят от 4 до 12 месяцев.", "c-food", "b-royal-canin", 1890m, 20, Korm, 4.8, false, true, D(17)),
        new("RC-004", "Royal Canin Instinctive, влажный корм в желе, 85 г", "Мягкие кусочки в желе для котов с избирательным аппетитом.", "c-food", "b-royal-canin", 99m, 80, Konservy, 4.6, false, true, D(16)),
        new("RC-005", "Royal Canin Light Weight Care, 400 г", "Выведен из ассортимента — демо неактивного товара каталога.", "c-food", "b-royal-canin", 620m, 10, Korm, 4.5, false, false, D(15)),
        new("GR-001", "Grandorf Adult, индейка с рисом, беззерновой корм, 2 кг", "Гипоаллергенный корм из индейки и риса для взрослых котов всех пород.", "c-food", "b-grandorf", 2890m, 10, Korm, 4.9, true, true, D(14)),
        new("GR-002", "Grandorf Weight Control, лосось с клюквой, 2 кг", "Низкокалорийный рацион для котов с склонностью к набору веса.", "c-food", "b-grandorf", 2650m, 8, Korm, 4.7, false, true, D(13)),
        new("GR-003", "Grandorf консервы, индейка и кролик, 95 г", "Влажный корм холистик из мяса индейки и кролика.", "c-food", "b-grandorf", 139m, 0, Konservy, 4.8, false, true, D(12)),
        new("MG-001", "Monge Natural Delight, тунец с лососем, 80 г", "Натуральные консервы без злаков — 70 % мяса рыбы в составе.", "c-food", "b-monge", 129m, 60, Konservy, 4.8, true, true, D(11)),
        new("MG-002", "Monge Cat Natural, тунец, 100 г", "Полнорационный влажный корм с единственным источником белка.", "c-food", "b-monge", 149m, 45, Konservy, 4.6, false, true, D(10)),
        new("MG-003", "Monge All Breed, курица, 1,5 кг", "Сухой корм для взрослых котов с курицей и рисом.", "c-food", "b-monge", 1750m, 12, Korm, 4.5, false, true, D(9)),
        new("BP-001", "Beaphar Duo Active Plus, паста для выведения шерсти, 100 г", "Двухцветная мальт-паста с пребиотиками против комков шерсти.", "c-food", "b-beaphar", 520m, 22, Vitamin, 4.7, false, true, D(9)),
        new("TR-001", "Triol лакомство «Мясные подушечки» с курицей, 60 г", "Мягкое лакомство для дрессировки и поощрения кота.", "c-food", "b-triol", 149m, 35, Konservy, 4.4, false, true, D(8)),
        new("EC-001", "Ever Clean Extra Strong, комкующийся наполнитель, 6 л", "Плотные комки и усиленный контроль запаха на срок до 10 дней.", "c-litter", "b-ever-clean", 890m, 25, Napolnitel, 4.8, true, true, D(20)),
        new("EC-002", "Ever Clean Unscented, без запаха, 10 л", "Гипоаллергенный наполнитель без отдушек для чувствительных котов.", "c-litter", "b-ever-clean", 1150m, 16, Napolnitel, 4.7, false, true, D(18)),
        new("EC-003", "Ever Clean Lavender, с ароматом лаванды, 6 л", "Комкующийся наполнитель с деликатным ароматом лаванды.", "c-litter", "b-ever-clean", 960m, 12, Napolnitel, 4.6, false, true, D(17)),
        new("EC-004", "Ever Clean Multiple Cats, для нескольких котов, 10 л", "Максимальный контроль запаха в доме с двумя и более котами.", "c-litter", "b-ever-clean", 1290m, 9, Napolnitel, 4.8, false, true, D(15)),
        new("TR-002", "Triol древесный наполнитель, 7 л", "Экологичный наполнитель из спрессованных опилок без пыли.", "c-litter", "b-triol", 420m, 30, Napolnitel, 4.3, false, true, D(14)),
        new("TR-003", "Triol силикагелевый наполнитель, 3,8 л", "Впитывает влагу и запах, экономичный расход.", "c-litter", "b-triol", 890m, 14, Napolnitel, 4.5, false, true, D(13)),
        new("TR-004", "Triol впитывающий наполнитель для котят, 5 л", "Мягкие гранулы для приучения котят к лотку.", "c-litter", "b-triol", 380m, 0, Napolnitel, 4.2, false, true, D(12)),
        new("TR-005", "Triol дразнилка «Пёрышко»", "Удочка с натуральным пером — охота без риска для рук хозяина.", "c-toys", "b-triol", 390m, 40, Draznilka, 4.8, true, true, D(21)),
        new("TR-006", "Мышка с кошачьей мятой, набор 2 шт", "Мягкие мышки с запасом котовника внутри.", "c-toys", "b-triol", 260m, 50, Mysh, 4.6, false, true, D(19)),
        new("TR-007", "Мячики с бубенчиком, набор 3 шт", "Лёгкие мячики для активных игр и апорта.", "c-toys", "b-triol", 210m, 45, Shar, 4.4, false, true, D(18)),
        new("TR-008", "Лазерная указка-брелок для игр с котом", "Три режима свечения, класс 2, безопасен для глаз.", "c-toys", "b-triol", 450m, 18, Shar, 4.3, false, true, D(16)),
        new("TR-009", "Интерактивная мышь на радиоуправлении", "Бегающая мышь с пультом — движение имитирует настоящую охоту.", "c-toys", "b-triol", 1290m, 6, Mysh, 4.7, true, true, D(15)),
        new("TR-010", "Игровой туннель, 90 см", "Шуршащий туннель с дыркой для подглядывания.", "c-toys", "b-triol", 890m, 11, Tunnel, 4.6, false, true, D(14)),
        new("TR-011", "Дразнилка-удочка с перьями и колокольчиком", "Телескопическая удочка до 90 см со сменными перьями.", "c-toys", "b-triol", 340m, 28, Draznilka, 4.5, false, true, D(13)),
        new("TR-012", "Triol щётка-пуходёрка для котов", "Двухсторонняя щётка: редкие и частые зубья для всех типов шерсти.", "c-care", "b-triol", 690m, 30, Shchetka, 4.8, true, true, D(19)),
        new("TR-013", "Triol щётка-рукавица для вычёсывания", "Массажная перчатка собирает шерсть одним движением.", "c-care", "b-triol", 450m, 24, Shchetka, 4.5, false, true, D(17)),
        new("TR-014", "Triol шампунь «Нежный» для котов, 250 мл", "Гипоаллергенный шампунь без слёз с протеинами шерсти.", "c-care", "b-triol", 380m, 20, Vitamin, 4.4, false, true, D(15)),
        new("TR-015", "Triol зубная паста для котов со вкусом курицы, 100 г", "Паста с энзимами для профилактики зубного камня.", "c-care", "b-triol", 350m, 26, Vitamin, 4.3, false, true, D(13)),
        new("TR-016", "Triol ошейник от блох и клещей", "Выведен из продаж — второй демо-пример неактивного товара.", "c-care", "b-triol", 590m, 10, Vitamin, 4.2, false, false, D(11)),
        new("BP-002", "Beaphar Ear Cleaner, капли для чистки ушей, 50 мл", "Мягкий лосьон для гигиены ушной раковины.", "c-care", "b-beaphar", 540m, 15, Vitamin, 4.6, false, true, D(10)),
        new("BP-003", "Beaphar Multi Vitamin, витамины для котов, 180 таблеток", "Комплекс витаминов и таурина для блеска шерсти и иммунитета.", "c-care", "b-beaphar", 890m, 12, Vitamin, 4.7, false, true, D(9)),
        new("TR-017", "Лежанка «Бублик» велюр, 55 см", "Мягкий бортик-подушка, в котором утонет даже самый пушистый кот.", "c-home", "b-triol", 1590m, 10, Lezhanka, 4.8, true, true, D(18)),
        new("TR-018", "Домик-будка с когтеточкой, 80 см", "Домик из плюша со столбом-когтеточкой из сизаля.", "c-home", "b-triol", 3990m, 5, Domik, 4.9, false, true, D(16)),
        new("TR-019", "Когтеточка напольная «Столбик», 60 см", "Столик с сизалевым столбом и игрушкой-помпоном.", "c-home", "b-triol", 1490m, 8, Domik, 4.6, false, true, D(15)),
        new("TR-020", "Гамак на батарею, 55 × 40 см", "Гамак с меховой подстилкой на радиатор — самое тёплое место дома.", "c-home", "b-triol", 890m, 14, Lezhanka, 4.5, false, true, D(14)),
        new("TR-021", "Лежанка-матрас Triol, 60 × 60 см", "Ровный матрас с антискользящим дном для лотка или пола.", "c-home", "b-triol", 990m, 16, Lezhanka, 4.4, false, true, D(12)),
        new("TR-022", "Домик-лежанка «Капля» со съёмной подушкой", "Закрытый домик-капсула из велюра для укрытия от суеты.", "c-home", "b-triol", 2790m, 4, Domik, 4.7, false, true, D(11)),
        new("TR-023", "Переноска Triol, 59 × 38 × 37 см", "Прочная переноска с металлической дверцей и вентиляцией.", "c-home", "b-triol", 2490m, 7, Perenoska, 4.6, false, true, D(10)),
    ];

    public static (string Id, string UserId, string? Label, string Recipient, string Phone, string City, string Street, string House, string? Apartment, string? Entrance, string? Floor, string? Intercom, string? PostalCode, bool IsDefault)[] Addresses =>
    [
        ("a-home", CustomerId, "Дом", "Анна Смирнова", CustomerPhone, "Москва", "ул. Лесная", "7", "24", "2", "5", "24", "125047", true),
        ("a-work", CustomerId, "Работа", "Анна Смирнова", CustomerPhone, "Москва", "ул. Тверская", "12", "3", null, null, null, "125009", false),
    ];

    public static (string Id, string Code, PromoType Type, decimal Value, decimal MinSubtotal, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int? MaxUses, int UsedCount, bool IsActive)[] Promos =>
    [
        ("p-mur10", "МУР10", PromoType.Percent, 10m, 1000m, new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2027, 12, 31, 23, 59, 59, TimeSpan.Zero), null, 3, true),
        ("p-500", "СКИДКА500", PromoType.Fixed, 500m, 3500m, new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2027, 6, 30, 23, 59, 59, TimeSpan.Zero), null, 0, true),
        ("p-old", "ПРОМО", PromoType.Percent, 15m, 0m, new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 8, 31, 23, 59, 59, TimeSpan.Zero), null, 12, true),
        ("p-limit", "ЛИМИТ", PromoType.Percent, 5m, 0m, new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2027, 12, 31, 23, 59, 59, TimeSpan.Zero), 10, 10, true),
    ];

    public static (string ProductId, int Quantity, DateTimeOffset AddedAt)[] CartItems =>
    [
        ("p-rc-sterilised", 1, new DateTimeOffset(2026, 10, 22, 9, 10, 0, TimeSpan.Zero)),
        ("p-ec-extra", 2, new DateTimeOffset(2026, 10, 22, 9, 12, 0, TimeSpan.Zero)),
        ("p-tr-feather", 1, new DateTimeOffset(2026, 10, 23, 18, 40, 0, TimeSpan.Zero)),
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
        new("o-1001", 1001, OrderStatus.Delivered, new DateTimeOffset(2026, 9, 18, 12, 30, 0, TimeSpan.Zero), null, null, 516m, 0m, DeliveryMethod.Courier, 290m,
        [
            ("p-mg-tuna", "Monge Natural Delight, тунец с лососем, 80 г", "MG-001", 129m, 4),
        ]),
        new("o-1002", 1002, OrderStatus.Confirmed, new DateTimeOffset(2026, 10, 5, 15, 5, 0, TimeSpan.Zero), "Позвонить за час до доставки", "МУР10", 1780m, 178m, DeliveryMethod.Courier, 290m,
        [
            ("p-ec-extra", "Ever Clean Extra Strong, комкующийся наполнитель, 6 л", "EC-001", 890m, 2),
        ]),
        new("o-1003", 1003, OrderStatus.New, new DateTimeOffset(2026, 10, 21, 10, 45, 0, TimeSpan.Zero), null, null, 1080m, 0m, DeliveryMethod.Pickup, 190m,
        [
            ("p-tr-feather", "Triol дразнилка «Пёрышко»", "TR-005", 390m, 1),
            ("p-tr-brush", "Triol щётка-пуходёрка для котов", "TR-012", 690m, 1),
        ]),
    ];

    public const int NextOrderNumber = 1004;
}
