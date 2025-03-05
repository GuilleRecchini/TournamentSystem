using TournamentSystem.Domain.Entities;

namespace TournamentSystem.DataAccess.Repositories
{
    public interface ICardRepository
    {
        Task<Card?> GetCardByIdAsync(int id);
        Task<IEnumerable<Card>?> GetCardsBySerieAsync(int id);
        Task<bool> DoAllCardsExistAsync(int[] cardIds);
        Task<IEnumerable<Card>> GetCardsByIdsAsync(int[] cardsIds);
        Task<IEnumerable<Card>> GetCardsByPlayerIdAsync(int playerId);
    }
}