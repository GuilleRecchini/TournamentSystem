using TournamentSystem.Domain.Entities;

namespace TournamentSystem.DataAccess.Repositories
{
    public interface IUserRepository
    {
        Task<int> CreateUserAsync(User user);
        Task<User> GetUserByIdAsync(int id);
    }
}