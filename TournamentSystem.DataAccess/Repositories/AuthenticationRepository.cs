using Dapper;
using Microsoft.Extensions.Options;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Configurations;

namespace TournamentSystem.DataAccess.Repositories
{
    public class AuthenticationRepository(IOptions<ConnectionStrings> options) : BaseRepository(options), IAuthenticationRepository
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

        public async Task<User> GetUserByEmail(string email)
        {
            const string query = @"
                SELECT * FROM Users 
                WHERE email = @Email;";

            var parameters = new { email };

            using var connection = CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<User>(query, parameters);
        }
    }
}
