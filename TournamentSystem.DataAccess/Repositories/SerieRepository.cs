using Dapper;
using Microsoft.Extensions.Options;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Configurations;

namespace TournamentSystem.DataAccess.Repositories
{
    public class SerieRepository(IOptions<ConnectionStrings> options) : BaseRepository(options), ISerieRepository
    {
        public async Task<List<Serie>> GetSeriesByIdsAsync(int[] serieIds)
        {
            const string query = @"
                SELECT *
                FROM series
                WHERE series_id IN @SerieIds";

            await using var connection = CreateConnection();
            return (await connection.QueryAsync<Serie>(query, new { SerieIds = serieIds })).ToList();
        }

        public async Task<bool> DoAllSeriesExistAsync(int[] serieIds)
        {
            const string query = @"
                SELECT COUNT(*) 
                FROM series
                WHERE series_id IN @SerieIds";

            await using var connection = CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(query, new { SerieIds = serieIds });
            return count == serieIds.Length;
        }

        public async Task<List<Serie>> GetAllSeriesAsync()
        {
            const string query = @"
                SELECT *
                FROM series";

            await using var connection = CreateConnection();
            return (await connection.QueryAsync<Serie>(query)).ToList();
        }

        public async Task<Serie> GetSerieByIdAsync(int serieId)
        {
            const string query = @"
                SELECT *
                FROM series
                WHERE series_id = @SerieId";

            await using var connection = CreateConnection();
            return await connection.QueryFirstAsync<Serie>(query, new { SerieId = serieId });
        }
    }
}
