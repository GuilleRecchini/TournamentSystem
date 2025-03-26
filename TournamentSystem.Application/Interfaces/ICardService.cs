using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Interfaces
{
    public interface ICardService
    {
        Task<Card?> GetCardByIdAsync(int id);
        Task<IEnumerable<Card>?> GetCardsBySerieAsync(int id);
    }
}