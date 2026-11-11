using AMZN.DTOs.Auth;
using AMZN.Services.Auth;
using AMZN.Shared.Exceptions;
using AMZN.Shared.Exceptions.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using AMZN.DTOs.Account;
using AMZN.Extensions;
using AMZN.Services.Account;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AMZN.Controllers.Api;

[Route("api/user")]
[ApiController]
public class UserAccountController : ControllerBase
{
    private readonly AccountService _accountService;

    public UserAccountController(
        AccountService accountService)
    {
        _accountService = accountService;
    }
    // POST api/user/update
    [HttpPost("update")]
    [Authorize]
    [EnableRateLimiting(RateLimitingExtensions.AccountApiPolicy)]
    public async Task<ActionResult<ProfileUpdateResponseDto>> Update([FromBody] ProfileUpdateRequestDto request)
    {
        var userId = User.GetRequiredUserId();
        ProfileUpdateResponseDto response = await _accountService.UpdateProfileAsync(request, userId);
        return StatusCode(StatusCodes.Status200OK, response);
    }
    
    // POST api/user/delete
    
    [HttpPost("delete")]
    [Authorize]
    [EnableRateLimiting(RateLimitingExtensions.AccountApiPolicy)]
    public async Task<ActionResult> DeleteUser()
    {
        var userId = User.GetRequiredUserId();
        await _accountService.DeleteUserAsync(userId);
        return StatusCode(StatusCodes.Status200OK);
    }
    
    // POST api/user/addPaymentMethod
    [HttpPost("addPaymentMethod")]
    [Authorize]
    [EnableRateLimiting(RateLimitingExtensions.AccountApiPolicy)]
    public async Task<ActionResult<PaymentMethodResponseDto>> AddPaymentMethod(
        [FromBody] PaymentMethodRequestDto request)
    {
        var userId = User.GetRequiredUserId();
        PaymentMethodResponseDto response = await _accountService.AddPaymentMethod(request, userId);
        return StatusCode(StatusCodes.Status200OK, response);
    }
    
    // GET api/user/getPaymentMethods
    [HttpGet("getPaymentMethods")]
    [Authorize]
    [EnableRateLimiting(RateLimitingExtensions.AccountApiPolicy)]
    public async Task<ActionResult<List<PaymentMethodResponseDto>>> GetPaymentMethods()
    {
        var userId = User.GetRequiredUserId();
        List<PaymentMethodResponseDto> response = await _accountService.GetUserPaymentMethods(userId);
        return StatusCode(StatusCodes.Status200OK, response);
    }
    
    // POST api/user/addDeliveryAddress
    [HttpPost("addDeliveryAddress")]
    [Authorize]
    [EnableRateLimiting(RateLimitingExtensions.AccountApiPolicy)]
    public async Task<ActionResult<DeliveryAddressResponseDto>> AddDeliveryAddress(
        [FromBody] DeliveryAddressRequestDto request)
    {
        var userId = User.GetRequiredUserId();
        DeliveryAddressResponseDto response = await _accountService.AddDeliveryAddress(request, userId);
        return StatusCode(StatusCodes.Status200OK, response);
    }
    
    // GET api/user/getDeliveryAddresses
    [HttpGet("getDeliveryAddresses")]
    [Authorize]
    [EnableRateLimiting(RateLimitingExtensions.AccountApiPolicy)]
    public async Task<ActionResult<List<DeliveryAddressResponseDto>>> GetDeliveryAddresses()
    {
        var userId = User.GetRequiredUserId();
        List<DeliveryAddressResponseDto> response = await _accountService.GetUserDeliveryAddresses(userId);
        return StatusCode(StatusCodes.Status200OK, response);
    }
}
