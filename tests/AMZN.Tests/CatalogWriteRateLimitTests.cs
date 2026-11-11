using System.Net;
using Xunit;

namespace AMZN.Tests;

public class CatalogWriteRateLimitTests
{
    // Один администратор отправляет пять запросов на создание бренда и пять на создание акции.
    // Оба действия расходуют общий лимит: следующая отправка каждой формы получает HTML 429.
    // Смена IP не сбрасывает лимит аккаунта, а другой аккаунт и формы загрузки имеют свои лимиты.
    [Fact]
    public async Task BrandAndPromotionCreation_ShareAccountLimitButNotUploadLimit()
    {
        using var host = new RateLimitTestHost();
        const string brandPath = "/Admin/Brands/Create";
        const string promotionPath = "/Admin/AddPromotion";
        const string firstAdmin = "11111111-1111-1111-1111-111111111111";
        const string secondAdmin = "22222222-2222-2222-2222-222222222222";

        await host.AssertAllowed(brandPath, 5, firstAdmin);
        await host.AssertAllowed(promotionPath, 5, firstAdmin);

        foreach (var path in new[] { brandPath, promotionPath })
        {
            using var response = await host.Post(path, firstAdmin, ip: "203.0.113.11");
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
            Assert.NotNull(response.Headers.RetryAfter);
            Assert.Contains("too many brand or promotion requests", await response.Content.ReadAsStringAsync());
        }
        Assert.Equal(10, host.AcceptedRequestCount);

        await host.AssertAllowed(brandPath, 1, secondAdmin);
        await host.AssertAllowed("/Admin/Products/Create", 3, firstAdmin);
    }
}
