using TournamentSystem.Domain.Entities;
using TournamentSystem.Domain.Enums;

namespace TournamentSystem.DataAccess.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<int> CreateUserAsync(User user);
        Task<bool> UserExistsByEmailOrAlias(string email, string alias);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> UsersExistByIdsAndRoleAsync(int[] usersIds, UserRole userRole);
    }
}