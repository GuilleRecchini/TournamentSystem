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
    }
}
