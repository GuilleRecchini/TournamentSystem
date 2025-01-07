using Dapper;
using Microsoft.Extensions.Configuration;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.DataAccess.Repositories
{
    public class UserRepository(IConfiguration configuration) : BaseRepository(configuration), IUserRepository
    {
        public async Task<User> GetUserByIdAsync(int id)
        {
            const string query = "SELECT * FROM Users WHERE user_id = @Id";

            var parameters = new { Id = id };

            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(query, parameters);
        }

        public async Task<int> CreateUserAsync(User user)
        {
            const string query = @"
                INSERT INTO Users 
                    (name, alias, email, password_hash, avatar_url, country_id, role, created_by) 
                VALUES 
                    (@Name, @Alias, @Email, @PasswordHash, @AvatarUrl, @CountryId, @Role, @CreatedBy); 
                SELECT LAST_INSERT_ID();";

            var parameters = new { user.Name, user.Alias, user.Email, user.PasswordHash, user.AvatarUrl, user.CountryId, Role = user.Role.ToString().ToLower(), user.CreatedBy };

            using var connection = CreateConnection();
            return await connection.QuerySingleAsync<int>(query, parameters);
        }
    }
}
