using TournamentSystem.Domain.Entities;

namespace TournamentSystem.DataAccess.Repositories
{
    public interface IPlayerRepository
    {
        Task<int> AddCardsToCollectionAsync(int[] cardIds, int playerId);
        Task<IEnumerable<Card>> GetCardsByPlayerIdAsync(int playerId);
    }
}