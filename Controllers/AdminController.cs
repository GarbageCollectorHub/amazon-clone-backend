using AMZN.DTOs.Auth;
using AMZN.Models.Action;
using AMZN.Models.Category;
using AMZN.Models.User;
using AMZN.Services.Admin;
using AMZN.Services.Auth;
using AMZN.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AMZN.Controllers;

public class AdminController : Controller
{
    private const string AccessTokenCookieName = "amzn_access";
    private const string RefreshTokenCookieName = "amzn_refresh";

    private readonly IAuthService _authService;
    private readonly ILogger<AdminController> _logger;
    private readonly AdminUserService _adminUserService;
    private readonly AdminActionService _adminActionService;

    public AdminController(
        IAuthService authService,
        ILogger<AdminController> logger,
        AdminUserService adminUserService,
        AdminActionService adminActionService)
    {
        _authService = authService;
        _logger = logger;
        _adminUserService = adminUserService;
        _adminActionService = adminActionService;
    }

    public IActionResult SignUp()
    {
        UserSignUpViewModel viewModel = new()
        {
            User = new()
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<JsonResult> Register(UserSignUpFormModel? model)
    {
        if (model == null)
        {
            return Json(new { success = false, message = "Form model is null" });
        }

        (bool success, object message) = await _adminUserService.RegisterUser(model);
        return Json(new { success, message });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("Auth")]
    public async Task<JsonResult> Login([FromForm] LoginRequestDto? request)
    {
        if (request == null || !ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Json(new
            {
                status = StatusCodes.Status400BadRequest,
                message = "Email and password are required"
            });
        }


        try
        {
            AuthResponseDto response = await _authService.LoginAsync(request);
            SetAuthCookies(response);

            TempData["Success"] = "Signed in successfully!";
            return Json(new { status = StatusCodes.Status200OK, message = "OK" });
        }
        catch (ApiException exception)
        {
            Response.StatusCode = exception.StatusCode;
            return Json(new { status = exception.StatusCode, message = exception.Message });
        }
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        string? refreshToken = Request.Cookies[RefreshTokenCookieName];
        try
        {
            await _authService.RevokeRefreshTokenAsync(refreshToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to revoke refresh token during MVC logout");
        }

        DeleteAuthCookies();
        TempData["Success"] = "Signed out successfully!";
        return RedirectToAction("Index", "Home");
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Category([FromServices] AdminCategoryService adminCategoryService)
    {
        CategoryViewModel viewModel = new()
        {
            Categories = adminCategoryService.GetAll()
        };
        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<JsonResult> AddCategory(
        CategoryFormModel? model,
        [FromServices] AdminCategoryService adminCategoryService)
    {
        if (model == null)
        {
            return Json(new { success = false, message = "Form model is null" });
        }

        try
        {
            (bool success, object message) = await adminCategoryService.AddCategoryAsync(model);
            return Json(new { success, message });
        }
        catch (InvalidOperationException exception)
        {
            return Json(new { success = false, message = exception.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Action()
    {
        ActionViewModel viewModel = new();
        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public JsonResult AddAction(ActionFormModel? model)
    {
        if (model == null)
        {
            return Json(new { success = false, message = "Form model is null" });
        }

        (bool success, object message) = _adminActionService.AddAction(model);
        return Json(new { success, message });
    }

    private void SetAuthCookies(AuthResponseDto response)
    {
        Response.Cookies.Append(
            AccessTokenCookieName,
            response.AccessToken,
            CreateCookieOptions(TimeSpan.FromSeconds(response.ExpiresInSeconds)));

        Response.Cookies.Append(
            RefreshTokenCookieName,
            response.RefreshToken,
            CreateCookieOptions(TimeSpan.FromSeconds(response.RefreshExpiresInSeconds)));
    }

    private CookieOptions CreateCookieOptions(TimeSpan maxAge)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = "/",
            MaxAge = maxAge
        };
    }

    private void DeleteAuthCookies()
    {
        var options = new CookieOptions
        {
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        };

        Response.Cookies.Delete(AccessTokenCookieName, options);
        Response.Cookies.Delete(RefreshTokenCookieName, options);
    }
}
