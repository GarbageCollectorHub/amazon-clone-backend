using AMZN.DTOs.Carts;
using AMZN.Services.Carts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using AMZN.Extensions;
namespace AMZN.Controllers.Api;

[Route("api/cart")]
[ApiController]

public class CartController : ControllerBase
{
    private readonly CartService _cartService;
    
    public CartController(CartService cartService)
    {
        _cartService = cartService;
    }
    
    // POST api/cart/add/{productId:guid}
    [HttpPost("add/{productId:guid}")]
    [Authorize]
    [EnableRateLimiting(RateLimitingExtensions.AccountApiPolicy)]
    public async Task<ActionResult<CartResponseDto>> AddToCart([FromRoute] Guid productId)
    {
        var userId = User.GetRequiredUserId();
        CartResponseDto response = await _cartService.AddToCartAsync(userId, productId);
        return StatusCode(StatusCodes.Status200OK, response);
    }
    
    // POST api/cart/remove/{productId:guid}
    [HttpPost("remove/{productId:guid}")]
    [Authorize]
    [EnableRateLimiting(RateLimitingExtensions.AccountApiPolicy)]
    public async Task<ActionResult<CartResponseDto>> RemoveFromCart([FromRoute] Guid productId)
    {
        var userId = User.GetRequiredUserId();
        CartResponseDto response = await _cartService.RemoveFromCartAsync(userId, productId);
        return StatusCode(StatusCodes.Status200OK, response);
    }
    
    // POST api/cart/clear
    [HttpPost("clear")]
    [Authorize]
    [EnableRateLimiting(RateLimitingExtensions.AccountApiPolicy)]
    public async Task<ActionResult<CartResponseDto>> ClearCart()
    {
        var userId = User.GetRequiredUserId();
        CartResponseDto response = await _cartService.ClearCartAsync(userId);
        return StatusCode(StatusCodes.Status200OK, response);
    }
    
    // GET api/cart/get
    [HttpGet("get")]
    [Authorize]
    [EnableRateLimiting(RateLimitingExtensions.AccountApiPolicy)]
    public async Task<ActionResult<CartResponseDto>> GetCart()
    {
        var userId = User.GetRequiredUserId();
        CartResponseDto response = await _cartService.GetCartAsync(userId);
        return StatusCode(StatusCodes.Status200OK, response);
    }
    
    // POST api/cart/increaseQuantity
    [HttpPost("increaseQuantity/{productId}")]
    [Authorize]
    [EnableRateLimiting(RateLimitingExtensions.AccountApiPolicy)]
    public async Task<ActionResult<CartResponseDto>> IncreaseQuantity([FromRoute] string productId)
    {
        var userId = User.GetRequiredUserId();
        if (string.IsNullOrEmpty(productId))
        {
            return BadRequest("Product id is required");
        }
        
        if (!Guid.TryParse(productId, out Guid parsedId))
        {
            return BadRequest("Product id is invalid");
        }
        
        CartResponseDto response = await _cartService.IncreaseQuantityAsync(userId, parsedId);
        return StatusCode(StatusCodes.Status200OK, response);
    }
    
    // POST / api/cart/decreaseQuantity
    [HttpPost("decreaseQuantity/{productId}")]
    [Authorize]
    [EnableRateLimiting(RateLimitingExtensions.AccountApiPolicy)]
    public async Task<ActionResult<CartResponseDto>> DecreaseQuantity([FromRoute] string productId)
    {
        var userId = User.GetRequiredUserId();
        if (string.IsNullOrEmpty(productId))
        {
            return BadRequest("Product id is required");
        }

        if (!Guid.TryParse(productId, out Guid parsedId))
        {
            return BadRequest("Product id is invalid");
        }
        
        CartResponseDto response = await _cartService.DecreaseQuantityAsync(userId, parsedId);
        return StatusCode(StatusCodes.Status200OK, response);
    }
}
