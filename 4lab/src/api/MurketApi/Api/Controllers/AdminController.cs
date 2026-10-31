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
    public async Task<ActionResult<PagedResult<ProductDto>>> Products(
        [FromQuery] string? search, [FromQuery] string? page, [FromQuery] string? pageSize) =>
        Ok(await admin.ProductsAsync(search, PageRequest.Normalize(Parse(page), Parse(pageSize))));

    /// <summary>Создать товар → 201; дубликат артикула — 409.</summary>
    [HttpPost("products")]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] ProductDraftDto draft) =>
        StatusCode(StatusCodes.Status201Created, await admin.CreateProductAsync(draft));

    /// <summary>Изменить товар.</summary>
    [HttpPut("products/{id}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(string id, [FromBody] ProductDraftDto draft) =>
        Ok(await admin.UpdateProductAsync(id, draft));

    /// <summary>Удалить товар (позиции удаляются из корзин; заказы хранят снимки).</summary>
    [HttpDelete("products/{id}")]
    public async Task<NoContentResult> DeleteProduct(string id)
    {
        await admin.DeleteProductAsync(id);
        return NoContent();
    }

    /// <summary>Заказы магазина с фильтром по статусу — постранично.</summary>
    [HttpGet("orders")]
    public async Task<ActionResult<PagedResult<OrderDto>>> Orders(
        [FromQuery] string? status, [FromQuery] string? page, [FromQuery] string? pageSize) =>
        Ok(await orders.AdminOrdersAsync(status, PageRequest.Normalize(Parse(page), Parse(pageSize))));

    /// <summary>Смена статуса заказа; при отмене остатки возвращаются на склад.</summary>
    [HttpPut("orders/{id}/status")]
    public async Task<ActionResult<OrderDto>> SetOrderStatus(string id, [FromBody] SetOrderStatusDto request) =>
        Ok(await orders.SetStatusAsync(id, request.Status));

    /// <summary>Пользователи системы.</summary>
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> Users() => Ok(await admin.UsersAsync());

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
