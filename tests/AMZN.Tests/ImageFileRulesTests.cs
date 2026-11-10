using AMZN.Shared.Validation.Files;
using Xunit;

namespace AMZN.Tests;

public class ImageFileRulesTests
{
    private const int MaxBytes = 5 * 1024 * 1024;

    // Разрешённые расширения и MIME-типы принимаются независимо от регистра.
    [Theory]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("photo.png", "image/png")]
    [InlineData("photo.webp", "image/webp")]
    [InlineData("PHOTO.JPG", "IMAGE/JPEG")]
    public void TryValidate_AllowedFormat_ReturnsTrue(string name, string contentType)
    {
        bool valid = ImageFileRules.TryValidate(TestImages.Create(name, contentType), MaxBytes, out string error);

        Assert.True(valid);
        Assert.Empty(error);
    }

    // Неподдерживаемое или отсутствующее расширение отклоняется даже при MIME-типе JPEG.
    [Theory]
    [InlineData("photo.svg")]
    [InlineData("photo.gif")]
    [InlineData("photo.bmp")]
    [InlineData("photo.mpeg")]
    [InlineData("photo.exe.jpg.exe")]
    [InlineData("photo")]
    public void TryValidate_UnsupportedExtension_ReturnsFalse(string name)
    {
        Assert.False(ImageFileRules.TryValidate(TestImages.Create(name), MaxBytes, out string error));
        Assert.NotEmpty(error);
    }

    // Разрешённого расширения недостаточно: пустой или неподдерживаемый MIME-тип отклоняется.
    [Theory]
    [InlineData("")]
    [InlineData("text/html")]
    [InlineData("application/octet-stream")]
    [InlineData("image/svg+xml")]
    public void TryValidate_MissingOrUnsupportedContentType_ReturnsFalse(string contentType)
    {
        Assert.False(ImageFileRules.TryValidate(TestImages.Create(contentType: contentType), MaxBytes, out string error));
        Assert.NotEmpty(error);
    }

    // Отсутствующий файл возвращает ошибку валидации, а не необработанное исключение.
    [Fact]
    public void TryValidate_MissingFile_ReturnsFalse()
    {
        Assert.False(ImageFileRules.TryValidate(null!, MaxBytes, out string error));
        Assert.NotEmpty(error);
    }

    // Пустой файл и превышение лимита на один байт отклоняются; ровно 5 МиБ разрешены.
    [Theory]
    [InlineData(0, false)]
    [InlineData(MaxBytes, true)]
    [InlineData(MaxBytes + 1, false)]
    public void TryValidate_FileSize_RespectsLimit(int size, bool expected)
    {
        bool valid = ImageFileRules.TryValidate(TestImages.Create(size: size), MaxBytes, out string error);

        Assert.Equal(expected, valid);
        Assert.Equal(expected, string.IsNullOrEmpty(error));
    }
}
