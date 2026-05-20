using AMZN.Data.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AMZN.Security.Tokens
{
    public class JwtService : IJwtService
    {
        private const int MinJwtKeyBytes = 32;
        private readonly IConfiguration _config;


        public JwtService(IConfiguration config)
        {
            _config = config;
        }



        public (string token, int expiresInSeconds) GenerateAccessToken(User user)
        {
            var keyBytes = GetJwtKeyBytes();
            var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);      // подпись JWT: HMAC-SHA256 + секретний ключ (keyBytes)

            var now = DateTime.UtcNow;
            var minutes = int.TryParse(_config["Jwt:ExpiresMinutes"], out var m) ? m : 30;      // если не задано в конфиге —> 30 минут
            var expires = now.AddMinutes(minutes);


            // claims юзера:  UserId/Email/Role
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),        // subject
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),          // .NET claim
                new Claim(ClaimTypes.Role, user.Role.ToString()),                  // Role
                new Claim(ClaimTypes.Email, user.Email),                           // Email

                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())  // jti   - id токена
            };


            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],      // iss
                audience: _config["Jwt:Audience"],  // aud
                claims: claims,                     // payload
                notBefore: now,                     // nbf: токен валиден не раньше этого времени (ставим now — валиден сразу)
                expires: expires,                   // exp: срок жизни токена
                signingCredentials: creds           // подпись токена (HS256 + secret key)
            );


            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            var expiresInSeconds = (int)TimeSpan.FromMinutes(minutes).TotalSeconds;

            return (jwt, expiresInSeconds);
        }



        public string GenerateRefreshToken()
        {
            // refresh token - случайная base64Url строка для обновления access токена
            return GenerateBase64UrlToken(32);
        }


        public string HashRefreshToken(string refreshToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)); 
            return Convert.ToHexString(bytes);
        }


        private byte[] GetJwtKeyBytes()
        {
            var keyBase64 = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing");

            byte[] keyBytes;
            try
            {
                keyBytes = Convert.FromBase64String(keyBase64);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Jwt:Key (signing key secret) must be a valid Base64 string", ex);
            }

            if (keyBytes.Length < MinJwtKeyBytes)
                throw new InvalidOperationException($"Jwt:Key (signing key secret) is too short. Need at least {MinJwtKeyBytes} bytes for HS256.");

            return keyBytes;
        }

        private static string GenerateBase64UrlToken(int bytesLength)
        {
            // base64Url (без + / =)
            var bytes = RandomNumberGenerator.GetBytes(bytesLength);

            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }




    }

}

