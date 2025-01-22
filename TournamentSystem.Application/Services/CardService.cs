using TournamentSystem.DataAccess.Repositories;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services
{
    public class CardService : ICardService
    {
        private readonly ICardRepository _cardRepository;

        public CardService(ICardRepository cardRepository)
        {
            _cardRepository = cardRepository;
        }

        public async Task<Card?> GetCardByIdAsync(int id)
        {
            return await _cardRepository.GetCardByIdAsync(id);
        }

        public async Task<IEnumerable<Card>?> GetCardsBySerieAsync(int id)
        {
            return await _cardRepository.GetCardsBySerieAsync(id);
        }
    }
}
