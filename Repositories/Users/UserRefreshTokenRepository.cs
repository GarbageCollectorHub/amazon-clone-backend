using AMZN.Data;
using AMZN.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AMZN.Repositories.Users
{
    public class UserRefreshTokenRepository : IUserRefreshTokenRepository
    {
        private readonly DataContext _db;

        public UserRefreshTokenRepository(DataContext db)
        {
            _db = db;
        }


        public async Task AddRefreshTokenAsync(UserRefreshToken token)
        {
            await _db.UserRefreshTokens.AddAsync(token);
            await _db.SaveChangesAsync();
        }

        public async Task<UserRefreshToken?> GetValidByHashAsync(string tokenHash)
        {
            return await _db.UserRefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.TokenHash == tokenHash &&
                    x.IsRevoked == false &&
                    x.ExpiresAt > DateTime.UtcNow);
        }

        public async Task<bool> TryRevokeAsync(string tokenHash)
        {
            int affectedRows = await _db.UserRefreshTokens
                .Where(x => x.TokenHash == tokenHash && !x.IsRevoked && x.ExpiresAt > DateTime.UtcNow)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsRevoked, true));

            return affectedRows == 1;
        }
    }
}
