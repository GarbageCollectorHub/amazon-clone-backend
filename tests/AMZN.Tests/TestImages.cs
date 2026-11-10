using Microsoft.AspNetCore.Http;

namespace AMZN.Tests;

internal static class TestImages
{
    // Создаёт файл с заданными метаданными и размером, без чтения изображений с диска.
    public static IFormFile Create(string name = "product.jpg", string contentType = "image/jpeg", int size = 10)
    {
        return new FormFile(new MemoryStream(new byte[size]), 0, size, "image", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
