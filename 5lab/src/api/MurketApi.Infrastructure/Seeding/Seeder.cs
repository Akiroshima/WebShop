using Microsoft.Extensions.Logging;

using MurketApi.Application.Abstractions;
using MurketApi.Domain.Catalog;
using MurketApi.Domain.Cart;
using MurketApi.Domain.Orders;
using MurketApi.Domain.Promos;
using MurketApi.Domain.Users;
using MurketApi.Infrastructure.Persistence;

namespace MurketApi.Infrastructure.Seeding;

/// <summary>
/// Сид: выполняется синхронно при старте до обслуживания запросов, идемпотентен (повторный запуск
/// не дублирует и не изменяет существующие записи). В Development пароли по умолчанию разрешены;
/// в Production старт блокируется снаружи (guard SEC-001 в Program).
/// </summary>
public sealed class Seeder(
    InMemoryDb db,
    IPasswordHasher hasher,
    SeedOptions options,
    ILogger<Seeder> logger)
{
    public void Run(bool isDevelopment)
    {
        lock (db.Sync)
        {
            if (db.Users.Count > 0)
            {
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

            foreach (var (id, login, fullName, email, phone, password, role, createdAt) in DemoData.Users)
            {
                var seedLogin = login == "admin" ? options.AdminLogin : login;
                var seedPassword = role == UserRole.Admin ? adminPassword : customerPassword;
                db.Users.Add(User.Create(id, seedLogin, email, phone,
                    hasher.Hash(seedPassword, "seed"), fullName, role, createdAt));
            }

            foreach (var (id, name) in DemoData.Categories)
            {
                db.Categories.Add(new Category(id, name));
            }

            foreach (var (id, name) in DemoData.Brands)
            {
                db.Brands.Add(new Brand(id, name));
            }

            var idBySku = DemoData.ProductIds.ToDictionary(pair => pair.Sku, pair => pair.Id);
            foreach (var seedProduct in DemoData.Products)
            {
                db.Products.Add(Product.Create(
                    idBySku[seedProduct.Sku], seedProduct.Sku, seedProduct.Name, seedProduct.Description,
                    seedProduct.CategoryId, seedProduct.BrandId, seedProduct.Price, seedProduct.Stock,
                    seedProduct.ImageUrl, seedProduct.Rating, seedProduct.IsPopular, seedProduct.IsActive,
                    seedProduct.CreatedAt));
            }

            var demoEnabled = options.DemoData ?? isDevelopment;
            if (demoEnabled)
            {
                SeedDemoData();
            }

            logger.LogInformation(
                "Сид выполнен: пользователей {Users}, товаров {Products}, демо-набор {Demo}",
                db.Users.Count, db.Products.Count, demoEnabled ? "включён" : "выключен");
        }
    }

    private void SeedDemoData()
    {
        foreach (var (id, userId, label, recipient, phone, city, street, house, apartment, entrance, floor, intercom, postalCode, isDefault) in DemoData.Addresses)
        {
            db.Addresses.Add(Address.Create(id, userId, label, recipient, phone, city, street, house,
                apartment, entrance, floor, intercom, postalCode, isDefault));
        }

        foreach (var (id, code, type, value, minSubtotal, startsAt, endsAt, maxUses, usedCount, isActive) in DemoData.Promos)
        {
            db.Promos.Add(PromoCode.Create(id, code, type, value, minSubtotal, startsAt, endsAt, maxUses, usedCount, isActive));
        }

        var cart = ShoppingCart.Create(DemoData.CustomerId);
        foreach (var (productId, quantity, addedAt) in DemoData.CartItems)
        {
            var product = db.Products.First(item => item.Id == productId);
            cart.AddItem(productId, quantity, product.Stock, addedAt);
        }

        db.Carts[DemoData.CustomerId] = cart;

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

            db.Orders.Add(order);
        }

        db.NextOrderNumber = DemoData.NextOrderNumber;
    }
}
