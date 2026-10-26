using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MurketApi.Application.Dtos;
using MurketApi.Application.Services;
using MurketApi.Domain.Common;

namespace MurketApi.Api.Controllers;

/// <summary>Заказы покупателя: оформление из корзины и «Мои заказы» (чужие — 404).</summary>
[ApiController]
[Route("api/v1/orders")]
[Authorize(Policy = "customer")]
public sealed class OrdersController(OrderService orders) : ControllerBase
{
    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

    /// <summary>Оформить заказ из корзины (повторная валидация остатков и промокода) → 201.</summary>
    [HttpPost]
    public ActionResult<CheckoutResultDto> Checkout([FromBody] CheckoutRequestDto request)
    {
        var result = orders.Checkout(UserId, request);
        return CreatedAtAction(nameof(UserOrder), new { id = result.Id }, result);
    }

    /// <summary>Свои заказы — постранично ({items, total, page, pageSize}).</summary>
    [HttpGet]
    public ActionResult<PagedResult<OrderDto>> MyOrders(
        [FromQuery] string? page, [FromQuery] string? pageSize)
    {
        var request = PageRequest.Normalize(Parse(page, "page"), Parse(pageSize, "pageSize"));
        return Ok(orders.MyOrders(UserId, request));
    }

    /// <summary>Свой заказ; чужой или несуществующий — 404.</summary>
    [HttpGet("{id}")]
    public ActionResult<OrderDto> UserOrder(string id) => Ok(orders.UserOrder(UserId, id));

    private static int? Parse(string? raw, string field)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!int.TryParse(raw, out var value) || value < 1)
        {
            throw new ValidationException("Данные заполнены неверно",
                new Dictionary<string, string[]> { [field] = ["Целое число больше нуля"] });
        }

        return value;
    }
}
