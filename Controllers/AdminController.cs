using AMZN.DTOs.Auth;
using AMZN.Extensions;
using AMZN.Models.Promotions;
using AMZN.Models.Category;
using AMZN.Models.User;
using AMZN.Services.Admin;
using AMZN.Services.Auth;
using AMZN.Security.Tokens;
using AMZN.Shared.Exceptions;
using AMZN.Shared.Limits;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AMZN.Controllers;

public class AdminController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AdminController> _logger;
    private readonly AdminUserService _adminUserService;
    private readonly AdminPromotionService _adminPromotionService;

    public AdminController(
        IAuthService authService,
        ILogger<AdminController> logger,
        AdminUserService adminUserService,
        AdminPromotionService adminPromotionService)
    {
        _authService = authService;
        _logger = logger;
        _adminUserService = adminUserService;
        _adminPromotionService = adminPromotionService;
    }

    public IActionResult SignUp()
    {
        return View(new UserSignUpViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitingExtensions.RegistrationPolicy)]
    public async Task<IActionResult> Register([Bind(Prefix = "Form")] UserSignUpFormModel form)
    {
        if (ModelState.IsValid)
        {
            AddFormErrors(await _adminUserService.RegisterUser(form));
            if (ModelState.IsValid)
            {
                TempData["Success"] = "Account created successfully!";
                return RedirectToAction(nameof(SignUp));
            }
        }

        return View("SignUp", new UserSignUpViewModel { Form = form });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitingExtensions.LoginPolicy)]
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
        string? refreshToken = Request.Cookies[JwtConfiguration.RefreshTokenCookieName];
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

    [HttpGet("/Admin/Categories")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Category([FromServices] AdminCategoryService adminCategoryService)
    {
        CategoryViewModel viewModel = new()
        {
            Categories = await adminCategoryService.GetAllAsync()
        };
        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitingExtensions.ImageUploadsPolicy)]
    [RequestSizeLimit(ImageUploadLimits.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImageUploadLimits.MaxRequestBytes)]
    public async Task<IActionResult> AddCategory(
        [Bind(Prefix = "Form")] CategoryFormModel form,
        [FromServices] AdminCategoryService adminCategoryService)
    {
        if (ModelState.IsValid)
        {
            try
            {
                AddFormErrors(await adminCategoryService.AddCategoryAsync(form));
                if (ModelState.IsValid)
                {
                    TempData["Success"] = "Category added successfully!";
                    return RedirectToAction(nameof(Category));
                }
            }
            catch (InvalidOperationException exception)
            {
                ModelState.AddModelError("Form.Image", exception.Message);
            }
        }

        return View("Category", new CategoryViewModel
        {
            Form = form,
            Categories = await adminCategoryService.GetAllAsync()
        });
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Promotions()
    {
        PromotionViewModel viewModel = new();
        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitingExtensions.CatalogWritesPolicy)]
    public async Task<IActionResult> AddPromotion([Bind(Prefix = "Form")] PromotionFormModel form)
    {
        if (ModelState.IsValid)
        {
            AddFormErrors(await _adminPromotionService.AddPromotionAsync(form));
            if (ModelState.IsValid)
            {
                TempData["Success"] = "Promotion added successfully!";
                return RedirectToAction(nameof(Promotions));
            }
        }

        return View("Promotions", new PromotionViewModel { Form = form });
    }

    private void AddFormErrors(Dictionary<string, string> errors)
    {
        foreach (var error in errors)
            ModelState.AddModelError($"Form.{error.Key}", error.Value);
    }

    private void SetAuthCookies(AuthResponseDto response)
    {
        Response.Cookies.Append(
            JwtConfiguration.AccessTokenCookieName,
            response.AccessToken,
            CreateCookieOptions(TimeSpan.FromSeconds(response.ExpiresInSeconds)));

        Response.Cookies.Append(
            JwtConfiguration.RefreshTokenCookieName,
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

        Response.Cookies.Delete(JwtConfiguration.AccessTokenCookieName, options);
        Response.Cookies.Delete(JwtConfiguration.RefreshTokenCookieName, options);
    }
}
