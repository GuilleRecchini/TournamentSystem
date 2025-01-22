using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services
{
    public interface IPlayerService
    {
        Task<bool> AddCardsToCollectionAsync(int[] cardIds, int playerId);
        Task<IEnumerable<Card>> GetCardsByPlayerIdAsyncAsync(int playerId);
    }
}