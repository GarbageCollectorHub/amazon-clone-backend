namespace AMZN.Services.Storage.Cloud;

using Azure;
using AMZN.Shared.Validation.Files;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data.Common;


public class CloudStorageService : ICloudStorageService
{
    private const string ContainerName = "amzn-storage";
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private readonly BlobContainerClient? _containerClient;
    private readonly ILogger<CloudStorageService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IImageUploadQuota _uploadQuota;

    private BlobContainerClient ContainerClient => _containerClient
        ?? throw new InvalidOperationException("Image storage is not configured. Image uploads are unavailable.");

    public CloudStorageService(IConfiguration configuration, ILogger<CloudStorageService> logger, IImageUploadQuota uploadQuota)
    {
        _logger = logger;
        _configuration = configuration;
        _uploadQuota = uploadQuota;
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
        if (!_configuration.GetValue("ImageUploads:Enabled", true))
            throw new InvalidOperationException("Image uploads are currently disabled. Please try again later.");

        if (!ImageFileRules.TryValidate(file, MaxImageBytes, out var error))
            throw new InvalidOperationException(error);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blobName = $"{Guid.NewGuid():N}{ext}";

        BlobClient blob = ContainerClient.GetBlobClient(blobName);

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
        };

        // Reserve before any Blob call. Do not refund on failure: a lost response may hide a completed write.
        bool reserved;
        try
        {
            reserved = await _uploadQuota.TryReserveAsync(file.Length, cancellationToken);
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException)
        {
            _logger.LogWarning(exception, "Failed to reserve the monthly image upload allowance");
            throw new InvalidOperationException("Image uploads are unavailable. Please try again later.", exception);
        }

        if (!reserved)
            throw new InvalidOperationException("This image exceeds the remaining monthly upload allowance. Try a smaller image or come back next month.");

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

        if (_containerClient == null || !Uri.TryCreate(fileUrl, UriKind.Absolute, out var fileUri))
            return false;

        var containerUri = new Uri(_containerClient.Uri.GetLeftPart(UriPartial.Path).TrimEnd('/') + "/");
        if (!containerUri.IsBaseOf(fileUri))
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
