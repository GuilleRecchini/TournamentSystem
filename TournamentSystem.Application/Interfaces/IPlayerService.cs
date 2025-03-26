using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Interfaces
{
    public interface IPlayerService
    {
        Task<int> AddCardsToCollectionAsync(int[] cardIds, int playerId);
        Task<IEnumerable<Card>> GetCardsByPlayerIdAsyncAsync(int playerId);
    }
}