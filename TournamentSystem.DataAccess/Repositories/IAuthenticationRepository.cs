using TournamentSystem.Domain.Entities;

namespace TournamentSystem.DataAccess.Repositories
{
    public interface IAuthenticationRepository
    {
        Task<int> CreateUserAsync(User user);
        Task<User> GetUserByEmail(string email);
    }
}