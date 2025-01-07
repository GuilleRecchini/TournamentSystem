using Dapper;
using Microsoft.Extensions.Options;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Configurations;

namespace TournamentSystem.DataAccess.Repositories
{
    public class UserRepository(IOptions<ConnectionStrings> options) : BaseRepository(options), IUserRepository
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
