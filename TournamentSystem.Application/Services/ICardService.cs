using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services
{
    public interface ICardService
    {
        Task<Card?> GetCardByIdAsync(int id);
        Task<IEnumerable<Card>?> GetCardsBySerieAsync(int id);
    }
}