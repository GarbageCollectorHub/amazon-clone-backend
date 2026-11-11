using AMZN.DTOs.Auth;
using AMZN.Services.Auth;
using AMZN.Extensions;
using AMZN.Shared.Exceptions;
using AMZN.Shared.Exceptions.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace AMZN.Controllers.Api
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }


        // POST api/auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingExtensions.RegistrationPolicy)]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
        {

            AuthResponseDto response = await _authService.RegisterAsync(request);
            return StatusCode(StatusCodes.Status201Created, response);
        }


        // POST api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingExtensions.LoginPolicy)]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody]  LoginRequestDto request)
        {

            AuthResponseDto response = await _authService.LoginAsync(request);
            return Ok(response);
        }


        // POST api/auth/refresh
        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingExtensions.RefreshPolicy)]
        public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshRequestDto request)
        {

            AuthResponseDto response = await _authService.RefreshAsync(request);
            return Ok(response);
        }


        // GET api/auth/me
        [HttpGet("me")]
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.AccountApiPolicy)]
        public ActionResult<MeResponseDto> Me()
        {
            string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string? emailValue  = User.FindFirstValue(ClaimTypes.Email);
            string? roleValue   = User.FindFirstValue(ClaimTypes.Role);

            if (!Guid.TryParse(userIdValue, out Guid userId))
                throw new ApiException(ErrorCodes.AuthClaimsInvalid, "Invalid auth claims", StatusCodes.Status401Unauthorized);

            if (string.IsNullOrWhiteSpace(emailValue) || string.IsNullOrWhiteSpace(roleValue))
                throw new ApiException(ErrorCodes.AuthClaimsInvalid, "Invalid auth claims", StatusCodes.Status401Unauthorized);

            return Ok(new MeResponseDto
            {
                Id = userId,
                Email = emailValue,
                Role = roleValue
            });
        }


    }
}
