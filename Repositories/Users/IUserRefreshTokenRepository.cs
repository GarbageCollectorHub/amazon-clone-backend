using AMZN.Data.Entities;

namespace AMZN.Repositories.Users
{
    public interface IUserRefreshTokenRepository
    {
        Task AddRefreshTokenAsync(UserRefreshToken token);
        Task<UserRefreshToken?> GetValidByHashAsync(string tokenHash);
        Task<bool> TryRevokeAsync(string tokenHash);


    }
}
