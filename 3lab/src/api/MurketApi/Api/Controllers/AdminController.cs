using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MurketApi.Application.Dtos;
using MurketApi.Application.Services;
using MurketApi.Domain.Common;

namespace MurketApi.Api.Controllers;

/// <summary>Раздел «Управление» (только администратор): товары, заказы, пользователи.</summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "admin")]
public sealed class AdminController(AdminService admin, OrderService orders) : ControllerBase
{
    /// <summary>Все товары (включая неактивные) с поиском — постранично.</summary>
    [HttpGet("products")]
    public ActionResult<PagedResult<ProductDto>> Products(
        [FromQuery] string? search, [FromQuery] string? page, [FromQuery] string? pageSize) =>
        Ok(admin.Products(search, PageRequest.Normalize(Parse(page), Parse(pageSize))));

    /// <summary>Создать товар → 201; дубликат артикула — 409.</summary>
    [HttpPost("products")]
    public ActionResult<ProductDto> CreateProduct([FromBody] ProductDraftDto draft) =>
        StatusCode(StatusCodes.Status201Created, admin.CreateProduct(draft));

    /// <summary>Изменить товар.</summary>
    [HttpPut("products/{id}")]
    public ActionResult<ProductDto> UpdateProduct(string id, [FromBody] ProductDraftDto draft) =>
        Ok(admin.UpdateProduct(id, draft));

    /// <summary>Удалить товар (позиции удаляются из корзин; заказы хранят снимки).</summary>
    [HttpDelete("products/{id}")]
    public NoContentResult DeleteProduct(string id)
    {
        admin.DeleteProduct(id);
        return NoContent();
    }

    /// <summary>Заказы магазина с фильтром по статусу — постранично.</summary>
    [HttpGet("orders")]
    public ActionResult<PagedResult<OrderDto>> Orders(
        [FromQuery] string? status, [FromQuery] string? page, [FromQuery] string? pageSize) =>
        Ok(orders.AdminOrders(status, PageRequest.Normalize(Parse(page), Parse(pageSize))));

    /// <summary>Смена статуса заказа; при отмене остатки возвращаются на склад.</summary>
    [HttpPut("orders/{id}/status")]
    public ActionResult<OrderDto> SetOrderStatus(string id, [FromBody] SetOrderStatusDto request) =>
        Ok(orders.SetStatus(id, request.Status));

    /// <summary>Пользователи системы.</summary>
    [HttpGet("users")]
    public ActionResult<IReadOnlyList<AdminUserDto>> Users() => Ok(admin.Users());

    private static int? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!int.TryParse(raw, out var value) || value < 1)
        {
            throw new ValidationException("Данные заполнены неверно",
                new Dictionary<string, string[]> { ["page"] = ["Целое число больше нуля"] });
        }

        return value;
    }
}
