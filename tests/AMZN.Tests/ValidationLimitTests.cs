using System.ComponentModel.DataAnnotations;
using System.Reflection;
using AMZN.Data;
using AMZN.Data.Entities;
using AMZN.DTOs.Auth;
using AMZN.DTOs.Products;
using AMZN.DTOs.Products.Questions;
using AMZN.DTOs.Products.Reviews;
using AMZN.Models.Product;
using AMZN.Models.User;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AMZN.Tests;

public class ValidationLimitTests
{
    // Сравниваем ограничение входного поля с длиной столбца в модели EF.
    // Оба должны совпадать с действующим контрактом; подключение к SQL не открываем.
    [Theory]
    [InlineData(typeof(ProductCreateFormModel), nameof(Product.Title), typeof(Product), 256)]
    [InlineData(typeof(ProductEditFormModel), nameof(Product.Title), typeof(Product), 256)]
    [InlineData(typeof(ReviewRequestDto), nameof(ProductReview.Title), typeof(ProductReview), 120)]
    [InlineData(typeof(ReviewRequestDto), nameof(ProductReview.Text), typeof(ProductReview), 4000)]
    [InlineData(typeof(ProductQuestionRequestDto), nameof(ProductQuestion.Text), typeof(ProductQuestion), 1024)]
    [InlineData(typeof(ProductQuestionAnswerRequestDto), nameof(ProductQuestionAnswer.Text), typeof(ProductQuestionAnswer), 2048)]
    public void InputLength_MatchesDatabaseColumn(Type inputType, string propertyName, Type entityType, int expectedLength)
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=ValidationModelOnly;Integrated Security=True")
            .Options;
        using var db = new DataContext(options);
        var inputProperty = inputType.GetProperty(propertyName)!;
        var entityProperty = db.Model.FindEntityType(entityType)!.FindProperty(propertyName)!;

        int? inputMaxLength = inputProperty.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength
            ?? inputProperty.GetCustomAttribute<MaxLengthAttribute>()?.Length;

        Assert.Equal(expectedLength, inputMaxLength);
        Assert.Equal(expectedLength, entityProperty.GetMaxLength());
    }

    // Вход, регистрация и создание пользователя в админке имеют разные лимиты.
    // Проверяем, что перенос в общие константы не изменил существующие контракты.
    [Theory]
    [InlineData(typeof(LoginRequestDto), "Email", 0, 100)]
    [InlineData(typeof(RegisterRequestDto), "Email", 0, 128)]
    [InlineData(typeof(ProfileUpdateRequestDto), "Email", 0, 128)]
    [InlineData(typeof(LoginRequestDto), "Password", 0, 72)]
    [InlineData(typeof(RegisterRequestDto), "Password", 6, 72)]
    [InlineData(typeof(UserSignUpFormModel), "Password", 8, 20)]
    public void UserFields_KeepTheirExistingLimits(Type inputType, string field, int min, int max)
    {
        var length = inputType.GetProperty(field)!.GetCustomAttribute<StringLengthAttribute>()!;

        Assert.Equal(min, length.MinimumLength);
        Assert.Equal(max, length.MaximumLength);
    }

    // Формы и фильтры должны принимать цену ровно в миллион и отклонять превышение даже на цент.
    [Theory]
    [InlineData(typeof(ProductCreateFormModel), nameof(ProductCreateFormModel.CurrentPrice))]
    [InlineData(typeof(ProductCreateFormModel), nameof(ProductCreateFormModel.OriginalPrice))]
    [InlineData(typeof(ProductEditFormModel), nameof(ProductEditFormModel.CurrentPrice))]
    [InlineData(typeof(ProductEditFormModel), nameof(ProductEditFormModel.OriginalPrice))]
    [InlineData(typeof(ProductListQueryDto), nameof(ProductListQueryDto.MinPrice))]
    [InlineData(typeof(ProductListQueryDto), nameof(ProductListQueryDto.MaxPrice))]
    public void ProductPrices_EnforceTheSameDecimalMaximum(Type inputType, string field)
    {
        var context = new ValidationContext(Activator.CreateInstance(inputType)!) { MemberName = field };
        var errors = new List<ValidationResult>();

        Assert.True(Validator.TryValidateProperty(999_999.99m, context, errors));
        Assert.True(Validator.TryValidateProperty(1_000_000m, context, errors));
        Assert.False(Validator.TryValidateProperty(1_000_000.01m, context, errors));
        Assert.Single(errors);
    }

    // Проверяем обе границы рейтинга на запросах оценки и отзыва. За пределами диапазона
    // обе формы отклоняют значение и показывают фактические границы в тексте ошибки.
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void RatingInputs_UseTheSameBoundsAndMessage(byte value, bool expectedValid)
    {
        foreach (object input in new object[] { new SetProductRatingRequestDto(), new ReviewRequestDto() })
        {
            var errors = new List<ValidationResult>();
            var context = new ValidationContext(input) { MemberName = nameof(ReviewRequestDto.Rating) };

            Assert.Equal(expectedValid, Validator.TryValidateProperty(value, context, errors));
            if (!expectedValid)
                Assert.Equal("Rating must be between 1 and 5", Assert.Single(errors).ErrorMessage);
        }
    }

    // Текст на один символ длиннее лимита вопроса отклоняется. Сообщение должно показать
    // предел вопроса (1024), а не более высокий предел, предназначенный для ответов.
    [Fact]
    public void QuestionLengthError_ShowsQuestionLimit()
    {
        var request = new ProductQuestionRequestDto { Text = new string('a', 1025) };
        var errors = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        Assert.Equal("Text must be between 5 and 1024 characters", Assert.Single(errors).ErrorMessage);
    }
}
