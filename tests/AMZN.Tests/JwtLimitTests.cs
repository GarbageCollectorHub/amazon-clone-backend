using AMZN.Data.Entities;
using AMZN.Security.Tokens;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AMZN.Tests;

public class JwtLimitTests
{
    // Используем вымышленный ключ, на байт короче допустимого для HS256. Общая проверка
    // настроек должна отклонить его до выпуска токена и показать требуемую длину.
    [Fact]
    public void GenerateAccessToken_RejectsSigningKeyBelowMinimum()
    {
        var service = CreateService(31);

        var error = Assert.Throws<InvalidOperationException>(() => service.GenerateAccessToken(CreateUser()));

        Assert.Contains("at least 32 bytes", error.Message);
    }

    // Ключ ровно 32 байта должен работать. Без переопределения в конфигурации
    // access token по-прежнему действует 30 минут.
    [Fact]
    public void GenerateAccessToken_AcceptsMinimumKeyAndKeepsDefaultLifetime()
    {
        var service = CreateService(32);

        var (token, expiresInSeconds) = service.GenerateAccessToken(CreateUser());

        Assert.NotEmpty(token);
        Assert.Equal(30 * 60, expiresInSeconds);
    }

    private static JwtService CreateService(int keyBytes)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:Key"] = Convert.ToBase64String(new byte[keyBytes]),
                ["Jwt:Issuer"] = "amzn-test",
                ["Jwt:Audience"] = "amzn-test"
            }).Build();
        return new JwtService(configuration);
    }

    private static User CreateUser() => new() { Id = Guid.NewGuid(), Email = "test@example.com" };
}
