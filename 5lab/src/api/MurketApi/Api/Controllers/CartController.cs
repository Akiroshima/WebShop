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
    public ActionResult<CartStateDto> Get() => Ok(cart.Get(UserId));

    /// <summary>Добавить товар {productId, quantity}; превышение остатка — ограничение с заметкой.</summary>
    [HttpPost("items")]
    public ActionResult<CartMutationResultDto> Add([FromBody] AddCartItemDto request) =>
        Ok(cart.Add(UserId, request.ProductId, request.Quantity));

    /// <summary>Изменить количество позиции (0 и меньше — удаление).</summary>
    [HttpPut("items/{productId}")]
    public ActionResult<CartMutationResultDto> SetQuantity(string productId,
        [FromBody] SetCartItemQuantityDto request) => Ok(cart.SetQuantity(UserId, productId, request.Quantity));

    /// <summary>Удалить позицию.</summary>
    [HttpDelete("items/{productId}")]
    public ActionResult<CartMutationResultDto> Remove(string productId) => Ok(cart.Remove(UserId, productId));

    /// <summary>Применить промокод {code} (не более одного на корзину).</summary>
    [HttpPost("promo")]
    public ActionResult<CartMutationResultDto> ApplyPromo([FromBody] ApplyPromoDto request) =>
        Ok(cart.ApplyPromo(UserId, request.Code));

    /// <summary>Убрать промокод.</summary>
    [HttpDelete("promo")]
    public ActionResult<CartMutationResultDto> RemovePromo() => Ok(cart.RemovePromo(UserId));
}
