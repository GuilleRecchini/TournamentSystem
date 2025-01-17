using Dapper;
using Microsoft.Extensions.Options;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Configurations;

namespace TournamentSystem.DataAccess.Repositories
{
    public class UserRepository(IOptions<ConnectionStrings> options) : BaseRepository(options), IUserRepository
    {
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

        public async Task<bool> UpdateUserAsync(User user)
        {
            const string query = @"
                UPDATE Users 
                SET 
                    name = @Name,
                    alias = @Alias,
                    email = @Email,                    
                    avatar_url = @AvatarUrl,
                    country_id = @CountryId                    
                WHERE user_id = @UserId;";

            var parameters = new
            {
                user.Name,
                user.Alias,
                user.Email,
                user.AvatarUrl,
                user.CountryId,
                user.UserId,
            };

            using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            const string query = @"
                DELETE FROM Users
                WHERE user_id = @UserId;";

            var parameters = new { UserId = userId };

            using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, parameters) > 0;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            const string query = "SELECT * FROM Users WHERE user_id = @Id";

            var parameters = new { Id = id };

            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(query, parameters);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            const string query = @"
                SELECT * FROM Users 
                WHERE email = @Email;";

            var parameters = new { email };

            using var connection = CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<User?>(query, parameters);
        }

        public async Task<bool> UserExistsByEmailOrAlias(string email, string alias)
        {
            const string query = @"
                SELECT COUNT(1) 
                FROM Users 
                WHERE email = @Email OR alias = @Alias;";

            var parameters = new { Email = email, Alias = alias };

            using var connection = CreateConnection();
            var userCount = await connection.QuerySingleAsync<int>(query, parameters);

            return userCount > 0;
        }
    }
}
