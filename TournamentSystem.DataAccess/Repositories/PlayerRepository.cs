using Dapper;
using Microsoft.Extensions.Options;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Configurations;

namespace TournamentSystem.DataAccess.Repositories
{
    public class PlayerRepository(IOptions<ConnectionStrings> options) : BaseRepository(options), IPlayerRepository
    {
        public async Task<bool> AddCardsToCollectionAsync(int[] cardIds, int playerId)
        {
            const string query = @"
                INSERT IGNORE INTO player_cards 
                    (user_id, card_id) 
                SELECT 
                    @PlayerId, card_id 
                FROM cards
                WHERE card_id IN @CardIds";

            var parameters = new { PlayerId = playerId, CardIds = cardIds };

            using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, parameters) > 0;
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
            using var connection = CreateConnection();
            return await connection.QueryAsync<Card>(query, parameters);

        }
    }
}
