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

            await using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Card>(query, parameters);
        }

        public async Task<List<Card>> GetCardsByIdsWithSeriesAsync(int[] cardsIds)
        {
            const string query = @"
                SELECT *
                FROM cards c
                JOIN card_series cs ON c.card_id = cs.card_id
                JOIN series s on cs.series_id = s.series_id
                WHERE c.card_id IN @CardsIds";

            await using var connection = CreateConnection();

            var cardsDictionary = new Dictionary<int, Card>();

            var cards = await connection.QueryAsync<Card, Serie, Card>(
                query,
                (card, series) =>
                {
                    if (!cardsDictionary.TryGetValue(card.CardId, out var cardEntry))
                    {
                        cardEntry = card;
                        cardEntry.Series = [];
                        cardsDictionary.Add(cardEntry.CardId, cardEntry);
                    }
                    if (series is not null && !cardEntry.Series.Any(c => c.SeriesId == series.SeriesId))
                    {
                        cardEntry.Series.Add(series);
                    }
                    return cardEntry;
                },
                new { CardsIds = cardsIds },
                splitOn: "series_id");

            return cardsDictionary.Values.ToList();
        }

        public async Task<bool> DoAllCardsExistAsync(int[] cardIds)
        {
            const string query = @"
                SELECT COUNT(*) 
                FROM cards
                WHERE card_id IN @CardIds";

            await using var connection = CreateConnection();
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

            await using var connection = CreateConnection();
            return await connection.QueryAsync<Card>(query, parameters);
        }
    }
}
