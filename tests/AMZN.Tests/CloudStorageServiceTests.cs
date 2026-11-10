using AMZN.Services.Storage.Cloud;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AMZN.Tests;

public class CloudStorageServiceTests
{
    // Отключение загрузок отклоняет даже допустимый файл до проверки настройки Blob.
    [Fact]
    public async Task SaveImageAsync_UploadsDisabled_RejectsBeforeAccessingStorage()
    {
        var storage = CreateStorage(uploadsEnabled: false);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => storage.SaveImageAsync(TestImages.Create()));

        Assert.Contains("uploads are currently disabled", error.Message);
    }

    // Вызов storage-сервиса без файла явно отклоняется как неверный аргумент.
    [Fact]
    public async Task SaveImageAsync_MissingFile_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => CreateStorage().SaveImageAsync(null!));
    }

    // Сервис сам проверяет формат и размер: неверный файл отклоняется до доступа к Blob.
    [Theory]
    [InlineData("photo.svg", "image/svg+xml", 10, "extension")]
    [InlineData("photo.jpg", "text/html", 10, "content type")]
    [InlineData("photo.jpg", "image/jpeg", 0, "empty")]
    [InlineData("photo.jpg", "image/jpeg", 5 * 1024 * 1024 + 1, "too large")]
    public async Task SaveImageAsync_InvalidFile_RejectsBeforeAccessingStorage(
        string name, string contentType, int size, string expectedError)
    {
        var storage = CreateStorage();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => storage.SaveImageAsync(TestImages.Create(name, contentType, size)));

        Assert.Contains(expectedError, error.Message);
    }

    // Допустимый файл проходит валидацию, но без настройки Blob возвращается ошибка конфигурации.
    [Fact]
    public async Task SaveImageAsync_ValidFileWithoutStorage_ReturnsConfigurationError()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateStorage().SaveImageAsync(TestImages.Create()));

        Assert.Contains("storage is not configured", error.Message);
    }

    private static CloudStorageService CreateStorage(bool uploadsEnabled = true)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ImageUploads:Enabled"] = uploadsEnabled.ToString()
            })
            .Build();

        return new CloudStorageService(configuration, NullLogger<CloudStorageService>.Instance);
    }
}
