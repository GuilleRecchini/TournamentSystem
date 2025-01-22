using Dapper;
using Microsoft.Extensions.Options;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Configurations;

namespace TournamentSystem.DataAccess.Repositories
{
    public class CardRepository(IOptions<ConnectionStrings> options) : BaseRepository(options), ICardRepository
    {
        public async Task<Card?> GetCardByIdAsync(int id)
        {
            const string query = "SELECT * FROM Cards WHERE card_id = @Id";

            var parameters = new { Id = id };

            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Card>(query, parameters);
        }

        public async Task<bool> DoAllCardsExistAsync(int[] cardIds)
        {
            const string query = @"
                SELECT COUNT(*) 
                FROM cards
                WHERE card_id IN @CardIds";

            using var connection = CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(query, new { CardIds = cardIds });
            return count == cardIds.Length;
        }


        public async Task<IEnumerable<Card>?> GetCardsBySerieAsync(int id)
        {
            const string query = @"
                SELECT 
                    c.*
                FROM 
                    cards c
                JOIN 
                    card_series cs ON c.card_id = cs.card_id
                JOIN 
                    series s ON cs.series_id = s.series_id
                WHERE 
                    s.series_id = @SeriesId
            ";

            var parameters = new { SeriesId = id };

            using var connection = CreateConnection();
            return await connection.QueryAsync<Card>(query, parameters);
        }
    }
}
