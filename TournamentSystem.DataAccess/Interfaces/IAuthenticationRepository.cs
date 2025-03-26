using TournamentSystem.Domain.Entities;

namespace TournamentSystem.DataAccess.Interfaces
{
    public interface IAuthenticationRepository
    {
        Task AddRefreshTokenAsync(RefreshToken refreshToken);
        Task<RefreshToken> GetRefreshTokenAsync(string token);
        Task DeleteRefreshTokenAsync(string token);
    }
}