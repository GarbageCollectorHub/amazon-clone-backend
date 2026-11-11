using AMZN.Services.Storage.Cloud;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AMZN.Tests;

public class CloudStorageServiceTests
{
    // Выключаем ImageUploads:Enabled и передаём допустимый файл без настроенной строки подключения.
    // Ожидаем именно ошибку отключённых загрузок: запрет должен сработать раньше доступа к Blob.
    [Fact]
    public async Task SaveImageAsync_UploadsDisabled_RejectsBeforeAccessingStorage()
    {
        var storage = CreateStorage(uploadsEnabled: false);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => storage.SaveImageAsync(TestImages.Create()));

        Assert.Contains("uploads are currently disabled", error.Message);
    }

    // Вызываем сервис записи без файла. Это ошибка вызывающего кода, поэтому ожидаем ArgumentNullException,
    // а не попытку подключения к хранилищу или случайную ошибку при обращении к свойствам файла.
    [Fact]
    public async Task SaveImageAsync_MissingFile_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => CreateStorage().SaveImageAsync(null!));
    }

    // Обращаемся прямо к сервису записи, обходя валидацию MVC-формы, и передаём неверный формат или размер.
    // Сервис должен сам отклонить файл с нужной причиной; без настроенного Blob это также проверяет порядок проверок.
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

    // Передаём допустимый файл, не задавая подключение к Azure. Проверки файла должны пройти,
    // после чего сервис возвращает понятное сообщение об отсутствии настройки хранилища.
    [Fact]
    public async Task SaveImageAsync_ValidFileWithoutStorage_ReturnsConfigurationError()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateStorage().SaveImageAsync(TestImages.Create()));

        Assert.Contains("storage is not configured", error.Message);
    }

    // Передаём валидный файл при исчерпанной квоте. Сервис должен учесть точный размер файла
    // и вернуть сообщение о квоте до обращения к Blob. Эмулятор для этого теста не запускаем.
    [Fact]
    public async Task SaveImageAsync_QuotaExhausted_RejectsBeforeCallingBlob()
    {
        var quota = new FullQuota();
        var storage = CreateStorage(quota: quota, storageConnectionString: "UseDevelopmentStorage=true");
        var file = TestImages.Create(size: 123);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => storage.SaveImageAsync(file));

        Assert.Contains("monthly upload allowance", error.Message);
        Assert.Equal(file.Length, quota.RequestedBytes);
    }

    private static CloudStorageService CreateStorage(bool uploadsEnabled = true,
        IImageUploadQuota? quota = null, string? storageConnectionString = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ImageUploads:Enabled"] = uploadsEnabled.ToString(),
                ["ConnectionStrings:AzureStorage"] = storageConnectionString
            })
            .Build();

        return new CloudStorageService(configuration, NullLogger<CloudStorageService>.Instance, quota ?? new UnexpectedQuota());
    }

    private sealed class UnexpectedQuota : IImageUploadQuota
    {
        public Task<bool> TryReserveAsync(long bytes, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Quota must not be accessed before file and storage checks.");
    }

    private sealed class FullQuota : IImageUploadQuota
    {
        public long RequestedBytes { get; private set; }

        public Task<bool> TryReserveAsync(long bytes, CancellationToken cancellationToken = default)
        {
            RequestedBytes = bytes;
            return Task.FromResult(false);
        }
    }
}
