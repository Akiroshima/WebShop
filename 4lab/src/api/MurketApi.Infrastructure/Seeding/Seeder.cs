using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using MurketApi.Application.Abstractions;
using MurketApi.Domain.Cart;
using MurketApi.Domain.Catalog;
using MurketApi.Domain.Orders;
using MurketApi.Domain.Promos;
using MurketApi.Domain.Users;
using MurketApi.Infrastructure.Persistence;

namespace MurketApi.Infrastructure.Seeding;

/// <summary>
/// Сид: выполняется при старте до обслуживания запросов, идемпотентен (при наличии пользователей
/// пропускается — повторный запуск не дублирует и не изменяет существующие записи). В Development
/// пароли по умолчанию разрешены; в Production старт блокируется снаружи (guard SEC-001 в Program).
/// Демо-набор идентичен SQL-скрипту src/api/db/03_seed_demo.sql (те же фиксированные uuid).
/// </summary>
public sealed class Seeder(
    MurketDbContext db,
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    IPasswordHasher hasher,
    SeedOptions options,
    ILogger<Seeder> logger)
{
    public async Task RunAsync(bool isDevelopment)
    {
        if (await db.Users.AnyAsync())
        {
            logger.LogInformation("Сид пропущен: в базе уже есть пользователи");
            return;
        }

        var adminPassword = options.AdminPassword
            ?? (isDevelopment ? "admin123" : null)
            ?? throw new InvalidOperationException(
                "Seed__AdminPassword: обязательна в Production (см. SEC-001 в README)");
        var customerPassword = options.CustomerPassword
            ?? (isDevelopment ? "customer123" : null)
            ?? throw new InvalidOperationException(
                "Seed__CustomerPassword: обязательна в Production (см. SEC-001 в README)");

        var demoEnabled = options.DemoData ?? isDevelopment;

        await unitOfWork.ExecuteAsync(async () =>
        {
            foreach (var (id, login, fullName, email, phone, _, role, createdAt) in DemoData.Users)
            {
                var seedLogin = login == "admin" ? options.AdminLogin : options.CustomerLogin;
                var seedPassword = role == UserRole.Admin ? adminPassword : customerPassword;
                db.Users.Add(User.Create(id, seedLogin, email, phone,
                    hasher.Hash(seedPassword, "seed"), fullName, role, createdAt));
            }

            db.Categories.AddRange(DemoData.Categories.Select(category => new Category(category.Id, category.Name)));
            db.Brands.AddRange(DemoData.Brands.Select(brand => new Brand(brand.Id, brand.Name)));

            var idBySku = DemoData.ProductIds.ToDictionary(pair => pair.Sku, pair => pair.Id);
            db.Products.AddRange(DemoData.Products.Select(seedProduct => Product.Create(
                idBySku[seedProduct.Sku], seedProduct.Sku, seedProduct.Name, seedProduct.Description,
                seedProduct.CategoryId, seedProduct.BrandId, seedProduct.Price, seedProduct.Stock,
                seedProduct.ImageUrl, seedProduct.Rating, seedProduct.IsPopular, seedProduct.IsActive,
                seedProduct.CreatedAt)));
            await db.SaveChangesAsync();

            if (demoEnabled)
            {
                await SeedDemoDataAsync();
            }

            logger.LogInformation(
                "Сид выполнен: пользователей {Users}, товаров {Products}, демо-набор {Demo}",
                await db.Users.CountAsync(), await db.Products.CountAsync(),
                demoEnabled ? "включён" : "выключен");
        });
    }

    private async Task SeedDemoDataAsync()
    {
        db.Addresses.AddRange(DemoData.Addresses.Select(address => Address.Create(
            address.Id, address.UserId, address.Label, address.Recipient, address.Phone,
            address.City, address.Street, address.House, address.Apartment, address.Entrance,
            address.Floor, address.Intercom, address.PostalCode, address.IsDefault)));
        db.Promos.AddRange(DemoData.Promos.Select(promo => PromoCode.Create(
            promo.Id, promo.Code, promo.Type, promo.Value, promo.MinSubtotal,
            promo.StartsAt, promo.EndsAt, promo.MaxUses, promo.UsedCount, promo.IsActive)));
        await db.SaveChangesAsync();

        var cart = ShoppingCart.Create(DemoData.CustomerId);
        foreach (var (productId, quantity, addedAt) in DemoData.CartItems)
        {
            var stock = await db.Products.AsNoTracking()
                .Where(product => product.Id == productId)
                .Select(product => product.Stock)
                .SingleAsync();
            cart.AddItem(productId, quantity, stock, addedAt);
        }

        db.Carts.Add(cart);
        await db.SaveChangesAsync();

        foreach (var seedOrder in DemoData.Orders)
        {
            var order = Order.Place(
                id: seedOrder.Id,
                number: seedOrder.Number,
                userId: DemoData.CustomerId,
                recipientName: "Анна Смирнова",
                recipientPhone: DemoData.CustomerPhone,
                recipientEmail: DemoData.CustomerEmail,
                address: AddressSnapshot.Create("Москва", "125047", "ул. Лесная", "7", "24", "2", "5", "24"),
                comment: seedOrder.Comment,
                deliveryMethod: seedOrder.DeliveryMethod,
                promoCode: seedOrder.PromoCode,
                subtotal: seedOrder.Subtotal,
                discount: seedOrder.Discount,
                items: [.. seedOrder.Items.Select(item => OrderItem.Create(
                    item.ProductId, item.Name, item.Sku, item.Price, item.Quantity))],
                createdAt: seedOrder.CreatedAt);
            // Place создаёт заказ в статусе New; доводим до статуса сида по допустимым переходам.
            if (seedOrder.Status == OrderStatus.Confirmed)
            {
                order.TransitionTo(OrderStatus.Confirmed);
            }
            else if (seedOrder.Status == OrderStatus.Delivered)
            {
                order.TransitionTo(OrderStatus.Confirmed);
                order.TransitionTo(OrderStatus.Delivered);
            }

            await orders.AddAsync(order);
        }

        // Демо-заказы вставлены с явными номерами — сдвигаем счётчик за последний из них.
        await orders.SetNextNumberAsync(DemoData.NextOrderNumber);
    }
}
