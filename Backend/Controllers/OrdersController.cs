using Ecommerce.Backend.DTOs;
using Ecommerce.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Backend.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController(OrderService orders) : ControllerBase
{
    /// <summary>
    /// Places a cash-on-delivery order from the current cart. Item prices and the total
    /// are calculated on the server; the request only carries delivery details.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var order = await orders.PlaceOrderAsync(User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpGet]
    public Task<List<OrderSummaryDto>> GetAll(CancellationToken ct) => orders.GetOrdersAsync(User.GetUserId(), ct);

    [HttpGet("{id:int}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<OrderDto> GetById(int id, CancellationToken ct) => orders.GetOrderAsync(User.GetUserId(), id, ct);
}
