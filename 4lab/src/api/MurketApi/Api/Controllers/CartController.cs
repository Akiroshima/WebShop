using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MurketApi.Application.Dtos;
using MurketApi.Application.Services;

namespace MurketApi.Api.Controllers;

/// <summary>Корзина текущего пользователя (требуется вход; гостевой корзины нет).</summary>
[ApiController]
[Route("api/v1/cart")]
[Authorize(Policy = "user")]
public sealed class CartController(CartService cart) : ControllerBase
{
    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

    /// <summary>Состояние корзины: позиции с текущими ценами, промокод, итоги.</summary>
    [HttpGet]
    public async Task<ActionResult<CartStateDto>> Get() => Ok(await cart.GetAsync(UserId));

    /// <summary>Добавить товар {productId, quantity}; превышение остатка — ограничение с заметкой.</summary>
    [HttpPost("items")]
    public async Task<ActionResult<CartMutationResultDto>> Add([FromBody] AddCartItemDto request) =>
        Ok(await cart.AddAsync(UserId, request.ProductId, request.Quantity));

    /// <summary>Изменение количества позиции (0 и меньше — удаление).</summary>
    [HttpPut("items/{productId}")]
    public async Task<ActionResult<CartMutationResultDto>> SetQuantity(string productId,
        [FromBody] SetCartItemQuantityDto request) =>
        Ok(await cart.SetQuantityAsync(UserId, productId, request.Quantity));

    /// <summary>Удалить позицию.</summary>
    [HttpDelete("items/{productId}")]
    public async Task<ActionResult<CartMutationResultDto>> Remove(string productId) =>
        Ok(await cart.RemoveAsync(UserId, productId));

    /// <summary>Применить промокод {code} (не более одного на корзину).</summary>
    [HttpPost("promo")]
    public async Task<ActionResult<CartMutationResultDto>> ApplyPromo([FromBody] ApplyPromoDto request) =>
        Ok(await cart.ApplyPromoAsync(UserId, request.Code));

    /// <summary>Убрать промокод.</summary>
    [HttpDelete("promo")]
    public async Task<ActionResult<CartMutationResultDto>> RemovePromo() =>
        Ok(await cart.RemovePromoAsync(UserId));
}
