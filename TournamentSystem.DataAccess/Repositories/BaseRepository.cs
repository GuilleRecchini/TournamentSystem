using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Data.Common;
using TournamentSystem.Infrastructure.Configurations;

namespace TournamentSystem.DataAccess.Repositories
{
    public abstract class BaseRepository
    {
        protected readonly string _connectionString;

        protected BaseRepository(IOptions<ConnectionStrings> options)
        {
            _connectionString = options.Value.DefaultConnection;
        }

        protected DbConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}
