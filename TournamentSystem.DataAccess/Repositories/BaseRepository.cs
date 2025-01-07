using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Data;

namespace TournamentSystem.DataAccess.Repositories
{
    public abstract class BaseRepository
    {
        protected readonly IConfiguration _configuration;

        protected BaseRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected IDbConnection CreateConnection()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            return new MySqlConnection(connectionString);
        }
    }
}
