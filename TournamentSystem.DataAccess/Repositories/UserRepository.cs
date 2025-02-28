using Dapper;
using Microsoft.Extensions.Options;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Domain.Enums;
using TournamentSystem.Infrastructure.Configurations;

namespace TournamentSystem.DataAccess.Repositories
{
    public class UserRepository(IOptions<ConnectionStrings> options) : BaseRepository(options), IUserRepository
    {
        public async Task<int> CreateUserAsync(User user)
        {
            const string query = @"
                INSERT INTO Users 
                    (name, alias, email, password_hash, avatar_url, country_code, role, created_by) 
                VALUES 
                    (@Name, @Alias, @Email, @PasswordHash, @AvatarUrl, @CountryCode, @Role, @CreatedBy); 
                SELECT LAST_INSERT_ID();";

            var parameters = new { user.Name, user.Alias, user.Email, user.PasswordHash, user.AvatarUrl, user.CountryCode, Role = user.Role.ToString().ToLower(), user.CreatedBy };

            await using var connection = CreateConnection();
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
                    country_code = @CountryCode                    
                WHERE user_id = @UserId;";

            var parameters = new
            {
                user.Name,
                user.Alias,
                user.Email,
                user.AvatarUrl,
                user.CountryCode,
                user.UserId,
            };

            await using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            const string query = @"
                DELETE FROM Users
                WHERE user_id = @UserId;";

            var parameters = new { UserId = userId };

            await using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, parameters) > 0;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            const string query = "SELECT * FROM Users WHERE user_id = @Id";

            var parameters = new { Id = id };

            await using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(query, parameters);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            const string query = @"
                SELECT * FROM Users 
                WHERE email = @Email;";

            var parameters = new { email };

            await using var connection = CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<User?>(query, parameters);
        }

        public async Task<bool> UserExistsByEmailOrAlias(string email, string alias)
        {
            const string query = @"
                SELECT COUNT(1) 
                FROM Users 
                WHERE email = @Email OR alias = @Alias;";

            var parameters = new { Email = email, Alias = alias };

            await using var connection = CreateConnection();
            var userCount = await connection.QuerySingleAsync<int>(query, parameters);

            return userCount > 0;
        }

        public async Task<bool> UsersExistByIdsAndRoleAsync(int[] usersIds, UserRole userRole)
        {
            const string query = @"
                    SELECT COUNT(user_id)
                    FROM Users
                    WHERE role = @UserRole 
                    AND user_id IN @UsersIds;";

            var parameters = new { UserRole = userRole.ToString().ToLower(), UsersIds = usersIds };
            await using var connection = CreateConnection();
            var userCount = await connection.QuerySingleAsync<int>(query, parameters);
            return userCount == usersIds.Length;
        }

    }
}
