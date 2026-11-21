using MurketApi.Application.Dtos;
using MurketApi.Domain.Catalog;
using MurketApi.Domain.Common;
using MurketApi.Domain.Orders;
using MurketApi.Domain.Promos;
using MurketApi.Domain.Services;
using MurketApi.Domain.Users;

namespace MurketApi.Application.Mapping;

/// <summary>Ручное отображение доменных сущностей в DTO контракта API.</summary>
public static class Mappers
{
    public static PublicUserDto ToPublicUser(this User user) =>
        new(user.Id, user.Login, user.FullName, user.Email, user.Phone, user.Role.ToApiString());

    public static AdminUserDto ToAdminUser(this User user) =>
        new(user.Id, user.Login, user.FullName, user.Email, user.Phone, user.Role.ToApiString(), user.CreatedAt);

    public static ProductDto ToProductDto(this Product product, IReadOnlyDictionary<string, string> categoryNames,
        IReadOnlyDictionary<string, string> brandNames) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Description,
        product.CategoryId,
        categoryNames.GetValueOrDefault(product.CategoryId, string.Empty),
        product.BrandId,
        product.BrandId is null ? null : brandNames.GetValueOrDefault(product.BrandId),
        product.Price,
        product.Stock,
        product.ImageUrl,
        product.Rating,
        product.IsPopular,
        product.IsActive,
        product.CreatedAt);

    public static CartStateDto ToCartState(this CartTotals totals, IReadOnlyDictionary<string, string> brandNames) => new(
        [.. totals.Lines.Select(line => new CartItemDto(
            line.Product.Id,
            line.Product.Name,
            line.Product.Sku,
            line.Product.BrandId is null ? null : brandNames.GetValueOrDefault(line.Product.BrandId),
            line.Product.Price,
            line.Product.Stock,
            line.Product.ImageUrl,
            line.Quantity,
            line.LineSubtotal))],
        totals.Lines.Sum(line => line.Quantity),
        totals.Subtotal,
        totals.AppliedPromo is null
            ? null
            : new AppliedPromoDto(
                totals.AppliedPromo.Code,
                totals.AppliedPromo.Type.ToApiString(),
                totals.AppliedPromo.Value,
                totals.Discount),
        totals.Discount,
        totals.Total);

    public static string ToApiString(this PromoType type) => type == PromoType.Percent ? "percent" : "fixed";

    public static OrderDto ToOrderDto(this Order order) => new(
        order.Id,
        order.Number,
        order.UserId,
        order.Status.ToApiString(),
        order.CreatedAt,
        order.RecipientName,
        order.RecipientPhone,
        order.RecipientEmail,
        new OrderAddressDto(
            order.Address.City,
            order.Address.PostalCode,
            order.Address.Street,
            order.Address.House,
            order.Address.Apartment,
            order.Address.Entrance,
            order.Address.Floor,
            order.Address.Intercom),
        order.Comment,
        order.PromoCode,
        order.Subtotal,
        order.Discount,
        order.DeliveryMethod.ToApiString(),
        order.DeliveryPrice,
        order.Total,
        [.. order.Items.Select(item => new OrderItemDto(
            item.ProductId, item.Name, item.Sku, item.UnitPrice, item.Quantity))]);

    public static string ToApiString(this OrderStatus status) => status switch
    {
        OrderStatus.New => "new",
        OrderStatus.Confirmed => "confirmed",
        OrderStatus.Delivered => "delivered",
        OrderStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static string ToApiString(this DeliveryMethod method) =>
        method == DeliveryMethod.Courier ? "courier" : "pickup";

    public static AddressDto ToAddressDto(this Address address) => new(
        address.Id,
        address.UserId,
        address.Label,
        address.Recipient,
        address.Phone,
        address.City,
        address.Street,
        address.House,
        address.Apartment,
        address.Entrance,
        address.Floor,
        address.Intercom,
        address.PostalCode,
        address.IsDefault);
}
