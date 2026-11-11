using System.Net;
using System.Text.Json;
using AMZN.Shared.Exceptions.Errors;
using Xunit;

namespace AMZN.Tests;

public class AuthenticationRateLimitTests
{
    // Отправляем запросы входа, регистрации или обновления токена с одного IP до исчерпания лимита.
    // Следующий запрос должен получить 429 (слишком много запросов) и Retry-After со временем ожидания.
    // Для регистрации через MVC ожидаем HTML-страницу, для API и модального входа - JSON с ошибкой.
    [Theory]
    [InlineData("/api/auth/login", 5, 60, false)]
    [InlineData("/Admin/Login", 5, 60, false)]
    [InlineData("/api/auth/register", 3, 600, false)]
    [InlineData("/Admin/Register", 3, 600, true)]
    [InlineData("/api/auth/refresh", 30, 60, false)]
    public async Task LimitReached_ReturnsExpectedResponse(string path, int limit, int windowSeconds, bool html)
    {
        using var host = new RateLimitTestHost();
        await host.AssertAllowed(path, limit);

        using var response = await host.Post(path);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        // Стенд считает запросы, пропущенные к тестовому ответу 200. Отклонённый запрос не должен туда попасть.
        Assert.Equal(limit, host.AcceptedRequestCount);
        Assert.InRange(response.Headers.RetryAfter!.Delta!.Value.TotalSeconds, windowSeconds - 10, windowSeconds);
        Assert.Equal(html ? "text/html" : "application/json", response.Content.Headers.ContentType!.MediaType);
        string body = await response.Content.ReadAsStringAsync();
        if (html)
        {
            Assert.Contains("too many registration requests", body);
            Assert.Contains("10 minutes", body);
        }
        else
        {
            using var json = JsonDocument.Parse(body);
            Assert.Equal(ErrorCodes.TooManyRequests, json.RootElement.GetProperty("code").GetString());
            Assert.Equal("Too many requests", json.RootElement.GetProperty("message").GetString());
            Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
        }
    }

    // С одного IP попеременно обращаемся к API и MVC: переключение интерфейса не должно давать новый лимит.
    // Когда общее число запросов достигнуто, оба адреса должны вернуть 429.
    [Theory]
    [InlineData("/api/auth/login", "/Admin/Login", 5)]
    [InlineData("/api/auth/register", "/Admin/Register", 3)]
    public async Task MvcAndApi_ShareOneCounter(string apiPath, string mvcPath, int limit)
    {
        using var host = new RateLimitTestHost();
        for (int attempt = 0; attempt < limit; attempt++)
            await host.AssertAllowed(attempt % 2 == 0 ? apiPath : mvcPath, 1);

        using var apiResponse = await host.Post(apiPath);
        using var mvcResponse = await host.Post(mvcPath);

        Assert.Equal(HttpStatusCode.TooManyRequests, apiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, mvcResponse.StatusCode);
    }

    // Сначала исчерпываем лимит чтения /api/auth/me, затем отдельно лимиты входа, регистрации и refresh.
    // У каждой операции должны оставаться все её попытки, даже когда предыдущие операции уже получают 429.
    [Fact]
    public async Task DifferentPolicies_HaveIndependentCounters()
    {
        using var host = new RateLimitTestHost();
        for (int attempt = 0; attempt < 10; attempt++)
        {
            using var response = await host.Client.GetAsync("/api/auth/me");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        using var rejectedMe = await host.Client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedMe.StatusCode);

        // В каждой паре указаны URL операции и число разрешённых запросов с одного IP.
        // Сначала расходуем все разрешённые попытки, затем проверяем отказ на следующей.
        foreach (var (path, allowedRequests) in new[] { ("/api/auth/login", 5), ("/api/auth/register", 3), ("/api/auth/refresh", 30) })
        {
            await host.AssertAllowed(path, allowedRequests);
            using var response = await host.Post(path);
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }
}
