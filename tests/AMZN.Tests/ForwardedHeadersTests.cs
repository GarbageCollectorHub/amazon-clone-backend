using System.Net;
using AMZN.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AMZN.Tests;

public class ForwardedHeadersTests
{
    // Имитируем запрос от доверенного proxy на localhost с адресом посетителя в X-Forwarded-For.
    // Middleware должен записать адрес посетителя в RemoteIpAddress и восстановить HTTPS из X-Forwarded-Proto.
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    public async Task TrustedProxy_ForwardsClientIpAndScheme(string proxyIp)
    {
        using var services = CreateServices();
        var context = CreateContext(services, proxyIp, "203.0.113.10");
        var pipeline = CreatePipeline(services);

        await pipeline(context);

        Assert.Equal(IPAddress.Parse("203.0.113.10"), context.Connection.RemoteIpAddress);
        Assert.Equal("https", context.Request.Scheme);
    }

    // Посетитель обращается напрямую с недоверенного для proxy адреса и присылает ложные заголовки.
    // Его RemoteIpAddress и HTTP-схема должны остаться исходными: доверять таким заголовкам нельзя.
    [Fact]
    public async Task UnknownProxy_CannotOverrideClientIpOrScheme()
    {
        using var services = CreateServices();
        var context = CreateContext(services, "203.0.113.10", "198.51.100.20");

        await CreatePipeline(services)(context);

        Assert.Equal(IPAddress.Parse("203.0.113.10"), context.Connection.RemoteIpAddress);
        Assert.Equal("http", context.Request.Scheme);
    }

    // В X-Forwarded-For стоят два адреса: подставленный посетителем слева и добавленный доверенным proxy справа.
    // Middleware должен выбрать правый адрес и остановиться, не принимая подставленный левый за IP посетителя.
    [Fact]
    public async Task ForwardedChain_StopsAtFirstUntrustedAddress()
    {
        using var services = CreateServices();
        var context = CreateContext(services, "127.0.0.1", "198.51.100.20, 203.0.113.10");

        await CreatePipeline(services)(context);

        Assert.Equal(IPAddress.Parse("203.0.113.10"), context.Connection.RemoteIpAddress);
    }

    // Два посетителя приходят через один proxy. Первый расходует 5 попыток входа и получает 429 на шестой.
    // У второго должны оставаться все 5 попыток: счётчик ведётся по IP посетителя, а не общему адресу proxy.
    [Fact]
    public async Task LoginLimit_UsesForwardedClientIpInsteadOfProxyIp()
    {
        using var services = CreateServices();
        var pipeline = CreatePipeline(services, rateLimiting: true);

        for (int attempt = 0; attempt < 5; attempt++)
            Assert.Equal(200, await SendLogin(services, pipeline, "127.0.0.1", "203.0.113.10"));

        Assert.Equal(429, await SendLogin(services, pipeline, "127.0.0.1", "203.0.113.10"));
        for (int attempt = 0; attempt < 5; attempt++)
            Assert.Equal(200, await SendLogin(services, pipeline, "127.0.0.1", "203.0.113.11"));

        Assert.Equal(429, await SendLogin(services, pipeline, "127.0.0.1", "203.0.113.11"));
        Assert.Equal(429, await SendLogin(services, pipeline, "127.0.0.1", "203.0.113.10"));
    }

    // Посетитель расходует лимит входа и добавляет разные ложные IP в начало X-Forwarded-For.
    // Адрес, добавленный доверенным proxy справа, не меняется, поэтому следующие запросы всё ещё получают 429.
    [Fact]
    public async Task LoginLimit_FakeForwardedPrefixCannotResetCounter()
    {
        using var services = CreateServices();
        var pipeline = CreatePipeline(services, rateLimiting: true);

        for (int attempt = 0; attempt < 5; attempt++)
            Assert.Equal(200, await SendLogin(services, pipeline, "127.0.0.1", "203.0.113.10"));

        Assert.Equal(429, await SendLogin(services, pipeline, "127.0.0.1", "198.51.100.20, 203.0.113.10"));
        Assert.Equal(429, await SendLogin(services, pipeline, "127.0.0.1", "198.51.100.21, 203.0.113.10"));
    }

    // Прямой клиент присылает новый X-Forwarded-For с каждой попыткой, но не является доверенным proxy.
    // Все запросы должны учитывать его исходный IP: после пяти попыток шестая получает 429.
    [Fact]
    public async Task LoginLimit_UnknownProxyCannotResetCounter()
    {
        using var services = CreateServices();
        var pipeline = CreatePipeline(services, rateLimiting: true);

        for (int attempt = 0; attempt < 5; attempt++)
            Assert.Equal(200, await SendLogin(services, pipeline, "203.0.113.10", $"198.51.100.{attempt + 1}"));

        Assert.Equal(429, await SendLogin(services, pipeline, "203.0.113.10", "198.51.100.99"));
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAmznForwardedHeaders();
        services.AddAmznRateLimiting();
        return services.BuildServiceProvider();
    }

    private static RequestDelegate CreatePipeline(IServiceProvider services, bool rateLimiting = false)
    {
        var app = new ApplicationBuilder(services);
        app.UseForwardedHeaders();
        if (rateLimiting)
            app.UseRateLimiter();
        app.Run(context => { context.Response.StatusCode = 200; return Task.CompletedTask; });
        return app.Build();
    }

    private static DefaultHttpContext CreateContext(IServiceProvider services, string remoteIp, string forwardedFor)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<int> SendLogin(IServiceProvider services, RequestDelegate pipeline, string remoteIp, string forwardedFor)
    {
        var context = CreateContext(services, remoteIp, forwardedFor);
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
            new EndpointMetadataCollection(new EnableRateLimitingAttribute(RateLimitingExtensions.LoginPolicy)), "Login"));
        await pipeline(context);
        return context.Response.StatusCode;
    }
}
