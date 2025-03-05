using Dapper;
using Microsoft.Extensions.Options;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Configurations;

namespace TournamentSystem.DataAccess.Repositories
{
    public class PlayerRepository(IOptions<ConnectionStrings> options) : BaseRepository(options), IPlayerRepository
    {
        public async Task<int> AddCardsToCollectionAsync(int[] cardIds, int playerId)
        {
            const string query = @"
                INSERT INTO player_cards 
                    (user_id, card_id) 
                SELECT 
                    @PlayerId, card_id 
                FROM cards
                WHERE card_id IN @CardIds
                AND NOT EXISTS (
                    SELECT 1 FROM player_cards WHERE user_id = @PlayerId AND card_id = cards.card_id
                )";

            var parameters = new { PlayerId = playerId, CardIds = cardIds };

            await using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, parameters);
        }

        public async Task<IEnumerable<Card>> GetCardsByPlayerIdAsync(int playerId)
        {
            const string query = @"
                SELECT 
                    c.*
                FROM player_cards AS pc
                JOIN cards AS c ON pc.card_id = c.card_id
                WHERE pc.user_id = @PlayerId";

            var parameters = new { PlayerId = playerId };
            await using var connection = CreateConnection();
            return await connection.QueryAsync<Card>(query, parameters);
        }
    }
}
