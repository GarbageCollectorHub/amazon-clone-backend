using System.ComponentModel.DataAnnotations;
using AMZN.Models.Category;
using AMZN.Models.Product;
using Xunit;

namespace AMZN.Tests;

public class ImageUploadFormTests
{
    private const int FileLimitBytes = 5 * 1024 * 1024;

    // Проверяем Data Annotations поля изображения в каждой форме: файл ровно 5 МиБ
    // допустим, а файл на байт больше отклоняется. Другие поля формы здесь не проверяются.
    [Theory]
    [InlineData(typeof(ProductCreateFormModel), nameof(ProductCreateFormModel.PrimaryImage))]
    [InlineData(typeof(ProductEditFormModel), nameof(ProductEditFormModel.NewPrimaryImage))]
    [InlineData(typeof(CategoryFormModel), nameof(CategoryFormModel.Image))]
    public void ImageField_RejectsFilesAboveSizeLimit(Type formType, string fieldName)
    {
        var context = new ValidationContext(Activator.CreateInstance(formType)!) { MemberName = fieldName };
        var errors = new List<ValidationResult>();

        Assert.True(Validator.TryValidateProperty(TestImages.Create(size: FileLimitBytes), context, errors));
        Assert.False(Validator.TryValidateProperty(TestImages.Create(size: FileLimitBytes + 1), context, errors));
        Assert.Single(errors);
    }

    // Передаём допустимые файлы в галереи создания и редактирования. Ровно десять файлов
    // проходят, а одиннадцать дают ошибку галереи независимо от размера отдельных файлов.
    [Theory]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void Gallery_EnforcesSameImageCountForCreateAndEdit(int imageCount, bool expectedValid)
    {
        var files = Enumerable.Repeat(TestImages.Create(), imageCount).ToList();
        var create = new ProductCreateFormModel { Images = files };
        var edit = new ProductEditFormModel { NewGalleryImages = files };

        Assert.Equal(expectedValid, !create.Validate(new ValidationContext(create)).Any());
        Assert.Equal(expectedValid, !edit.Validate(new ValidationContext(edit)).Any());
    }
}
