using AMZN.Shared.Validation.Files;
using Xunit;

namespace AMZN.Tests;

public class ImageFileRulesTests
{
    private const int MaxBytes = 5 * 1024 * 1024;

    // Передаём имя файла и MIME-тип каждого разрешённого формата, включая вариант с заглавными буквами.
    // Проверка должна принять эти метаданные и не вернуть ошибку; декодирование содержимого здесь не проверяется.
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

    // Файл объявлен как JPEG, но имя содержит неподдерживаемое расширение или не содержит расширения вообще.
    // Он должен быть отклонён: одного допустимого MIME-типа недостаточно для разрешения загрузки.
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

    // Имя заканчивается на разрешённое .jpg, но MIME-тип отсутствует или не входит в список допустимых.
    // Файл должен быть отклонён, а причина возвращена в error для показа ошибки пользователю.
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

    // Передаём null вместо файла. Правило валидации должно вернуть false и описание ошибки,
    // чтобы форма могла показать сообщение пользователю, а не упасть с исключением.
    [Fact]
    public void TryValidate_MissingFile_ReturnsFalse()
    {
        Assert.False(ImageFileRules.TryValidate(null!, MaxBytes, out string error));
        Assert.NotEmpty(error);
    }

    // Проверяем границы разрешённого размера: пустой файл и 5 МиБ + 1 байт должны отклоняться,
    // а файл ровно 5 МиБ - приниматься. Это защищает от ошибки сравнения на границе лимита.
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
