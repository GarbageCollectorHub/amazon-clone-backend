namespace AMZN.Services.Storage.Cloud;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;


public class CloudStorageService : ICloudStorageService
{
    private const string ContainerName = "amzn-storage";
    private readonly BlobContainerClient? _containerClient;
    private readonly ILogger<CloudStorageService> _logger;

    private BlobContainerClient ContainerClient => _containerClient
        ?? throw new InvalidOperationException("Image storage is not configured. Image uploads are unavailable.");

    public CloudStorageService(IConfiguration configuration, ILogger<CloudStorageService> logger)
    {
        _logger = logger;
        var connectionString = configuration.GetConnectionString("AzureStorage");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("Azure Blob Storage is not configured. Image uploads are unavailable.");
            return;
        }

        var options = new BlobClientOptions
        {
            Retry = { MaxRetries = 1, NetworkTimeout = TimeSpan.FromSeconds(5) }
        };
        _containerClient = new BlobContainerClient(connectionString, ContainerName, options);
    }

    public async Task<string> SaveImageAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null) throw new ArgumentNullException(nameof(file));
        if (file.Length <= 0) throw new InvalidOperationException("File is empty");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blobName = $"{Guid.NewGuid():N}{ext}";

        BlobClient blob = ContainerClient.GetBlobClient(blobName);

        var contentType = file.ContentType;

        // Если клиент не прислал ContentType то определяем по расширению (для корректного отображения в браузере)
        if (string.IsNullOrWhiteSpace(contentType))
        {
            contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
        }

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        try
        {
            await ContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

            await using Stream stream = file.OpenReadStream();
            await blob.UploadAsync(stream, options, cancellationToken);

            return blob.Uri.AbsoluteUri;
        }
        catch (Exception exception) when (exception is RequestFailedException or AggregateException)
        {
            _logger.LogWarning(exception, "Failed to upload an image to Azure Blob Storage");
            throw new InvalidOperationException("Image storage is unavailable. Please try again later.", exception);
        }
    }

    public async Task<Stream?> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        BlobClient blob = ContainerClient.GetBlobClient(fileName);

        if (!await blob.ExistsAsync(cancellationToken))
            return null;

        BlobDownloadResult download = await blob.DownloadContentAsync(cancellationToken);
        return download.Content.ToStream();
    }


    public async Task<bool> DeleteFileByUrlAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return false;

        string blobName = ExtractBlobName(fileUrl);
        return await DeleteFileByNameAsync(blobName, cancellationToken);
    }

    public async Task<bool> DeleteFileByNameAsync(string blobName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            return false;

        BlobClient blob = ContainerClient.GetBlobClient(blobName);
        Response<bool> result = await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);

        return result.Value;
    }

    // Из blob url достает имя файла (последний сегмент пути)
    private static string ExtractBlobName(string fileUrl)
    {
        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("Invalid blob url");

        return Uri.UnescapeDataString(uri.Segments.Last().Trim('/'));
    }

}
