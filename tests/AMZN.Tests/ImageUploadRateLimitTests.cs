using System.Net;
using Xunit;

namespace AMZN.Tests;

public class ImageUploadRateLimitTests
{
    private const string UserA = "11111111-1111-1111-1111-111111111111";
    private const string UserB = "22222222-2222-2222-2222-222222222222";

    // Один пользователь отправляет по одному запросу создания товара, редактирования товара и создания категории.
    // После этих трёх отправок любая из форм должна получить HTML-страницу 429 и время ожидания.
    // Файлы не отправляем: ограничение действует на отправку формы, а не только на наличие изображения.
    [Fact]
    public async Task UploadEndpoints_ShareAccountLimitAndReturnHtml()
    {
        using var host = new RateLimitTestHost();
        string[] paths = ["/Admin/Products/Create", RateLimitTestHost.EditPath, "/Admin/AddCategory"];
        foreach (string path in paths) await host.AssertAllowed(path, 1, UserA);

        foreach (string path in paths)
        {
            using var response = await host.Post(path, UserA);
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
            Assert.InRange(response.Headers.RetryAfter!.Delta!.Value.TotalSeconds, 50, 60);
            Assert.Contains("Your last request was not processed", await response.Content.ReadAsStringAsync());
        }
        Assert.Equal(3, host.AcceptedRequestCount);
    }

    // Пользователь A расходует три отправки, затем меняет IP: это не должно восстановить его лимит.
    // Пользователь B с исходного IP должен получить свои три отправки, не разделяя лимит аккаунта A.
    [Fact]
    public async Task UploadLimit_UsesAccountInsteadOfIp()
    {
        using var host = new RateLimitTestHost();
        const string path = "/Admin/Products/Create";
        await host.AssertAllowed(path, 3, UserA);

        using var rejected = await host.Post(path, UserA, "203.0.113.11");
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        await host.AssertAllowed(path, 3, UserB);
        using var otherRejected = await host.Post(path, UserB);
        Assert.Equal(HttpStatusCode.TooManyRequests, otherRejected.StatusCode);
    }

    // Проверяем запасной способ выбора счётчика: если идентификатора пользователя нет, используется IP.
    // После трёх отправок с первого IP второй IP должен сохранить свои попытки.
    // Это тест политики отдельно от авторизации; он не разрешает анонимную загрузку в настоящем приложении.
    [Fact]
    public async Task UploadLimit_WithoutUserClaim_FallsBackToIp()
    {
        using var host = new RateLimitTestHost();
        const string path = "/Admin/Products/Create";
        await host.AssertAllowed(path, 3);
        using var rejected = await host.Post(path);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        await host.AssertAllowed(path, 3, ip: "203.0.113.11");
    }

    // Несколько раз открываем форму и удаляем товар, затем отправляем форму создания товара.
    // Все три отправки должны оставаться доступными: GET и удаление не относятся к лимиту загрузок.
    [Fact]
    public async Task OtherActions_DoNotConsumeUploadLimit()
    {
        using var host = new RateLimitTestHost();
        for (int attempt = 0; attempt < 5; attempt++)
        {
            using var response = await host.Client.GetAsync("/Admin/Products/Create");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await host.AssertAllowed("/Admin/Products/Delete/11111111-1111-1111-1111-111111111111", 1, UserA);
        }
        await host.AssertAllowed("/Admin/Products/Create", 3, UserA);
        using var rejected = await host.Post("/Admin/Products/Create", UserA);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }
}
