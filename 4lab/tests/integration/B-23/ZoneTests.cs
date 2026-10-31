using System.Net;
using System.Net.Http.Json;
using MurketApi.Infrastructure.Seeding;
using MurketApi.TestKit;

namespace MurketApi.Integration.B23;

/// <summary>
/// B-23 · Сквозные проверки на реальной PostgreSQL: зона работает на собственной базе,
/// оформление атомарно, снимки заказа неизменяемы, каскады внешних ключей живы.
/// </summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    private static async Task<string> CheckoutAsync(HttpClient client, string extraProductId)
    {
        if (extraProductId is not null)
        {
            var add = await client.PostAsJsonAsync("/api/v1/cart/items",
                new { productId = extraProductId, quantity = 1 });
            ZoneApi.EnsureSuccess(add, "наполнение корзины");
        }

        var checkout = await client.PostAsJsonAsync("/api/v1/orders", new
        {
            recipientName = "Анна Смирнова",
            recipientPhone = "+79991234567",
            address = new { city = "Москва", street = "ул. Лесная", house = "7" },
            deliveryMethod = "courier",
        });
        ZoneApi.EnsureSuccess(checkout, "оформление заказа");
        return ZoneApi.String(await ZoneApi.BodyAsync(checkout), "id");
    }

    [Fact]
    public async Task ЗонаРаботаетНаСобственнойБазе_СДемоДанными()
    {
        var api = await Api;

        var users = await api.QueryAsync("SELECT count(*) FROM users");
        var products = await api.QueryAsync("SELECT count(*) FROM products");
        Assert.Equal(2L, users[0]);
        Assert.Equal(41L, products[0]);

        // HTTP-вход пишет refresh-токен именно в базу зоны — строка подключения дошла до приложения.
        using var client = await api.LoginAsync("customer", "customer123");
        var tokens = await api.QueryAsync("SELECT count(*) FROM refresh_tokens");
        Assert.True((long)tokens[0]! >= 1);
    }

    [Fact]
    public async Task Оформление_АтомарноСписываетОстатокИПромокод()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        // Состав корзины восстанавливается явно: тест не зависит от порядка методов зоны.
        foreach (var (sku, quantity) in new[] { ("RC-001", 1), ("EC-001", 2), ("TR-005", 1) })
        {
            var add = await client.PostAsJsonAsync("/api/v1/cart/items",
                new { productId = DemoData.ProductId(sku), quantity });
            ZoneApi.EnsureSuccess(add, $"наполнение корзины ({sku})");
        }

        var apply = await client.PostAsJsonAsync("/api/v1/cart/promo", new { code = "МУР10" });
        ZoneApi.EnsureSuccess(apply, "применение МУР10");

        var stockBefore = (int)(await api.QueryAsync(
            "SELECT stock FROM products WHERE id = @p0", Guid.Parse(DemoData.ProductId("EC-001"))))[0]!;
        var orderId = await CheckoutAsync(client, DemoData.ProductId("EC-001"));

        // Одна транзакция: заказ вставлен, остаток списан, used_count промокода вырос, корзина очищена.
        var stockAfter = (int)(await api.QueryAsync(
            "SELECT stock FROM products WHERE id = @p0", Guid.Parse(DemoData.ProductId("EC-001"))))[0]!;
        Assert.Equal(stockBefore - 3, stockAfter); // 2 позиции корзины + 1 добавленная

        var promo = await api.QueryAsync(
            "SELECT used_count FROM promos WHERE code = 'МУР10'");
        Assert.Equal(4, Convert.ToInt32(promo[0])); // сид: 3 использования

        var orderRow = await api.QueryAsync(
            "SELECT promo_id, subtotal, discount FROM orders WHERE id = @p0", Guid.Parse(orderId));
        Assert.NotNull(orderRow[0]);
        Assert.Equal(5350m, (decimal)orderRow[1]!); // 4460 + 890

        var cartRows = await api.QueryAsync("SELECT count(*) FROM cart_items");
        Assert.Equal(0L, cartRows[0]);
    }

    [Fact]
    public async Task СнимкиЗаказаНеМеняютсяПриИзмененииЦены()
    {
        var api = await Api;
        using var customer = await api.LoginAsync("customer", "customer123");
        using var admin = await api.LoginAsync("admin", "admin123");
        var orderId = await CheckoutAsync(customer, DemoData.ProductId("RC-001"));

        try
        {
            // Менеджер меняет цену и наличие уже после оформления.
            var change = await admin.PutAsJsonAsync($"/api/v1/admin/products/{DemoData.ProductId("RC-001")}",
                new
                {
                    sku = "RC-001",
                    name = "Royal Canin Sterilised 37, сухой корм для стерилизованных котов, 2 кг",
                    description = "Полнорационный корм.",
                    categoryId = DemoData.FoodCategoryId,
                    brandId = DemoData.RoyalCaninBrandId,
                    price = 9999m,
                    stock = 1,
                    imageUrl = "assets/img/products/korm.svg",
                    isActive = true,
                });
            ZoneApi.EnsureSuccess(change, "изменение цены после оформления");

            // Позиция заказа хранит снимок цены на момент покупки.
            var snapshot = await api.QueryAsync(
                "SELECT price FROM order_items WHERE order_id = @p0 AND sku = 'RC-001'",
                Guid.Parse(orderId));
            Assert.Equal(2290m, (decimal)snapshot[0]!);
        }
        finally
        {
            // Возврат демо-состояния: другие тесты зоны считают суммы с этой ценой.
            var restore = await admin.PutAsJsonAsync($"/api/v1/admin/products/{DemoData.ProductId("RC-001")}",
                new
                {
                    sku = "RC-001",
                    name = "Royal Canin Sterilised 37, сухой корм для стерилизованных котов, 2 кг",
                    description = "Полнорационный корм.",
                    categoryId = DemoData.FoodCategoryId,
                    brandId = DemoData.RoyalCaninBrandId,
                    price = 2290m,
                    stock = 14,
                    imageUrl = "assets/img/products/korm.svg",
                    isActive = true,
                });
            ZoneApi.EnsureSuccess(restore, "возврат демо-цены");
        }
    }

    [Fact]
    public async Task УдалениеТовара_ОчищаетКорзинуИПомечаетПозицииЗаказов()
    {
        var api = await Api;
        using var customer = await api.LoginAsync("customer", "customer123");
        using var admin = await api.LoginAsync("admin", "admin123");
        var orderId = await CheckoutAsync(customer, DemoData.ProductId("TR-012"));

        var removed = await admin.DeleteAsync($"/api/v1/admin/products/{DemoData.ProductId("TR-012")}");
        ZoneApi.EnsureSuccess(removed, "удаление товара");

        // Позиции корзин ссылаются на товары каскадом — строк больше нет.
        var cartRows = await api.QueryAsync(
            "SELECT count(*) FROM cart_items WHERE product_id = @p0", Guid.Parse(DemoData.ProductId("TR-012")));
        Assert.Equal(0L, cartRows[0]);

        // Позиция заказа сохраняет снимок: product_id обнулился (ON DELETE SET NULL), имя и цена живы.
        var item = await api.QueryAsync(
            "SELECT product_id, name, price FROM order_items WHERE order_id = @p0 AND sku = 'TR-012'",
            Guid.Parse(orderId));
        Assert.Equal(DBNull.Value, item[0]);
        Assert.Equal(690m, (decimal)item[2]!);
        Assert.False(string.IsNullOrEmpty((string)item[1]!));
    }

    [Fact]
    public async Task НомераЗаказов_УникальныИВозрастают()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");
        await CheckoutAsync(client, DemoData.ProductId("MG-001"));
        await CheckoutAsync(client, DemoData.ProductId("MG-002"));

        var duplicates = await api.QueryAsync(
            """
            SELECT count(*) FROM (
                SELECT number FROM orders GROUP BY number HAVING count(*) > 1
            ) d
            """);
        Assert.Equal(0L, duplicates[0]);

        var max = await api.QueryAsync("SELECT max(number) FROM orders");
        var seq = await api.QueryAsync("SELECT last_value FROM order_number_seq");
        Assert.True(Convert.ToInt32(seq[0]) >= Convert.ToInt32(max[0]));
    }
}
