using AMZN.Data.Entities;
using AMZN.Shared.Limits;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AMZN.Security.Tokens
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;


        public JwtService(IConfiguration config)
        {
            _config = config;
        }



        public (string token, int expiresInSeconds) GenerateAccessToken(User user)
        {
            var keyBytes = JwtConfiguration.ReadSigningKey(_config);
            var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);      // подпись JWT: HMAC-SHA256 + секретний ключ (keyBytes)

            var now = DateTime.UtcNow;
            var minutes = int.TryParse(_config[JwtConfiguration.ExpiresMinutesKey], out var m) ? m : JwtLimits.AccessTokenDefaultMinutes;
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
                issuer: _config[JwtConfiguration.IssuerKey],      // iss
                audience: _config[JwtConfiguration.AudienceKey],  // aud
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
            return GenerateBase64UrlToken(JwtLimits.RefreshTokenEntropyBytes);
        }


        public string HashRefreshToken(string refreshToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)); 
            return Convert.ToHexString(bytes);
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

