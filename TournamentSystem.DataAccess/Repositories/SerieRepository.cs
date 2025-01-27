using Dapper;
using Microsoft.Extensions.Options;
using TournamentSystem.Infrastructure.Configurations;

namespace TournamentSystem.DataAccess.Repositories
{
    public class SerieRepository(IOptions<ConnectionStrings> options) : BaseRepository(options), ISerieRepository
    {
        public async Task<bool> DoAllSeriesExistAsync(int[] serieIds)
        {
            const string query = @"
                SELECT COUNT(*) 
                FROM series
                WHERE series_id IN @SerieIds";

            using var connection = CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(query, new { SerieIds = serieIds });
            return count == serieIds.Length;
        }
    }
}
