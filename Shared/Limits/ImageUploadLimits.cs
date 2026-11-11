namespace AMZN.Shared.Limits;

public static class ImageUploadLimits
{
    private const int BytesPerMiB = 1024 * 1024;

    public const int MaxFileMiB = 5;
    public const int MaxFileBytes = MaxFileMiB * BytesPerMiB;

    // Includes all files and multipart form data.
    public const int MaxRequestBytes = 25 * BytesPerMiB;
    public const int MaxGalleryImages = 10;
}
