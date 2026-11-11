using System.Net;
using System.Reflection;
using System.Security.Claims;
using AMZN.Controllers;
using AMZN.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ApiAuthController = AMZN.Controllers.Api.AuthController;

namespace AMZN.Tests;

// Локальный HTTP-стенд: реальные middleware и Razor-страница 429, но вместо действий контроллеров - ответ 200.
// Так проверяем ограничения запросов без регистрации пользователей, работы с БД или загрузки файлов в Azure.
internal sealed class RateLimitTestHost : IDisposable
{
    public const string EditPath = "/Admin/Products/Edit/11111111-1111-1111-1111-111111111111";
    private readonly TestServer _server;
    public HttpClient Client { get; }
    public int AcceptedRequestCount { get; private set; }

    public RateLimitTestHost()
    {
        _server = new TestServer(new WebHostBuilder()
            .UseSetting(WebHostDefaults.ApplicationKey, typeof(AdminController).Assembly.GetName().Name)
            .ConfigureServices(services =>
            {
                services.AddControllersWithViews().AddApplicationPart(typeof(AdminController).Assembly);
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.AddAmznRateLimiting();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.Use(async (context, next) =>
                {
                    // Эти заголовки существуют только в тестовом стенде и задают IP и аккаунт вымышленного клиента.
                    context.Connection.RemoteIpAddress = IPAddress.Parse(context.Request.Headers["Test-IP"].FirstOrDefault() ?? "203.0.113.10");
                    string? userId = context.Request.Headers["Test-User"].FirstOrDefault();
                    if (userId != null)
                        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test"));
                    await next();
                });
                app.UseRateLimiter();
                app.UseEndpoints(endpoints =>
                {
                    void MapAction(string path, Type controller, string action, bool isPost = true)
                    {
                        // Берём атрибут с настоящего метода, чтобы тест заметил удаление или смену его политики.
                        Type verb = isPost ? typeof(HttpPostAttribute) : typeof(HttpGetAttribute);
                        var method = controller.GetMethods().Single(m => m.Name == action && m.IsDefined(verb));
                        var endpoint = isPost ? endpoints.MapPost(path, HandleRequest) : endpoints.MapGet(path, HandleRequest);
                        var policy = method.GetCustomAttribute<EnableRateLimitingAttribute>();
                        if (policy != null) endpoint.WithMetadata(policy);
                    }

                    MapAction("/api/auth/login", typeof(ApiAuthController), "Login");
                    MapAction("/Admin/Login", typeof(AdminController), "Login");
                    MapAction("/api/auth/register", typeof(ApiAuthController), "Register");
                    MapAction("/Admin/Register", typeof(AdminController), "Register");
                    MapAction("/api/auth/refresh", typeof(ApiAuthController), "Refresh");
                    MapAction("/Admin/Products/Create", typeof(AdminProductsController), "Create");
                    MapAction(EditPath, typeof(AdminProductsController), "Edit");
                    MapAction("/Admin/AddCategory", typeof(AdminController), "AddCategory");
                    MapAction("/Admin/Brands/Create", typeof(AdminBrandsController), "Create");
                    MapAction("/Admin/AddPromotion", typeof(AdminController), "AddPromotion");
                    MapAction("/Admin/Products/Delete/11111111-1111-1111-1111-111111111111", typeof(AdminProductsController), "Delete");
                    MapAction("/api/auth/me", typeof(ApiAuthController), "Me", isPost: false);
                    MapAction("/Admin/Products/Create", typeof(AdminProductsController), "Create", isPost: false);
                });
            }));
        Client = _server.CreateClient();
    }

    private Task HandleRequest(HttpContext context)
    {
        AcceptedRequestCount++;
        context.Response.StatusCode = StatusCodes.Status200OK;
        return Task.CompletedTask;
    }

    public async Task<HttpResponseMessage> Post(string path, string? user = null, string ip = "203.0.113.10")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("Test-IP", ip);
        if (user != null) request.Headers.Add("Test-User", user);
        return await Client.SendAsync(request);
    }

    public async Task AssertAllowed(string path, int count, string? user = null, string ip = "203.0.113.10")
    {
        // Каждый HTTP-ответ освобождаем сразу: серия запросов не должна удерживать ресурсы тестового сервера.
        for (int attempt = 0; attempt < count; attempt++)
        {
            using var response = await Post(path, user, ip);
            Xunit.Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    public void Dispose()
    {
        Client.Dispose();
        _server.Dispose();
    }
}
