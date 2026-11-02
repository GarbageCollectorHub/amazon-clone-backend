using AMZN.Data.Entities;
using AMZN.DTOs.Orders;
using AMZN.Services.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using AMZN.Extensions;
namespace AMZN.Controllers.Api;

[Route("api/order")]
[ApiController]

public class OrderController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrderController(OrderService orderService)
    {
        _orderService = orderService;
    }
    
    // POST api/order/create
    [HttpPost("create")]
    [Authorize]
    public async Task<ActionResult<OrderDto>> Checkout()
    {
        var user = User.GetRequiredUserId();
        OrderDto response = await _orderService.CreateOrderAsync(user);
        return StatusCode(StatusCodes.Status200OK, response);
    }
    
    // POST api/order/update
    [HttpPost("update")]
    [Authorize]
    public async Task<ActionResult<OrderDto>> UpdateOrder()
    {
        var user = User.GetRequiredUserId();
        OrderDto response = await _orderService.UpdateOrderAsync(user);
        return StatusCode(StatusCodes.Status200OK, response);
    }
    
    // POST api/order/confirm
    [HttpPost("confirm/{deliveryAddressId}/{paymentMethodId}")]
    [Authorize]
    public async Task<ActionResult<CompletedOrderDto>> ConfirmOrder([FromRoute]string deliveryAddressId,
        [FromRoute]string paymentMethodId)
    {
        if (string.IsNullOrEmpty(deliveryAddressId) || string.IsNullOrEmpty(paymentMethodId))
        {
            return BadRequest("Delivery address id and payment method id are required");
        }
        if (!Guid.TryParse(deliveryAddressId, out Guid addressParsed) || !Guid.TryParse(paymentMethodId, out Guid methodParsed))
        {
            return BadRequest("Delivery address id and / or payment method id are invalid");
        }
        var user = User.GetRequiredUserId();
        CompletedOrderDto response = await _orderService.CompleteOrderAsync(user, methodParsed, addressParsed);
        return StatusCode(StatusCodes.Status200OK, response);
    }
    
    // POST api/order/cancel
    [HttpPost("cancel")]
    [Authorize]
    public async Task<ActionResult<CompletedOrderDto>> CancelOrder()
    {
        var user = User.GetRequiredUserId();
        CompletedOrderDto response = await _orderService.CancelOrderAsync(user);
        return StatusCode(StatusCodes.Status200OK, response);
    }
    
    // GET api/order/getOrders
    [HttpGet("getOrders")]
    [Authorize]
    public async Task<ActionResult<List<CompletedOrderDto>>> GetOrders()
    {
        var user = User.GetRequiredUserId();
        List<CompletedOrderDto> response = await _orderService.GetOrdersAsync(user);
        return StatusCode(StatusCodes.Status200OK, response);
    }
}
